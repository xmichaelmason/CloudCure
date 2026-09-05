using CloudCure.Domain.Entities;
using CloudCure.Domain.Enums;
using CloudCure.Infrastructure.Audit;
using CloudCure.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace CloudCure.Application.Tests;

/// <summary>
/// Verifies the audit trail against a real Postgres — this is the regression test for the old
/// app having no audit trail at all despite handling PHI (see plan section 2).
/// </summary>
public class AuditTrailTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16")
        .WithDatabase("cloudcure_v2_test")
        .WithUsername("cloudcure")
        .WithPassword("cloudcure")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    private CloudCureDbContext CreateContext(ICurrentRequestContext? requestContext = null)
    {
        var options = new DbContextOptionsBuilder<CloudCureDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .UseSnakeCaseNamingConvention()
            .Options;

        return new CloudCureDbContext(options, requestContext ?? NullCurrentRequestContext.Instance);
    }

    [Fact]
    public async Task Inserting_an_audited_entity_writes_exactly_one_matching_audit_log_row()
    {
        await using var context = CreateContext();

        var person = new Person
        {
            Id = Guid.NewGuid(),
            FirstName = "Ada",
            LastName = "Lovelace",
            DateOfBirth = new DateOnly(1990, 1, 1),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        context.People.Add(person);

        await context.SaveChangesAsync();

        var auditRows = await context.AuditLog
            .Where(a => a.EntityType == nameof(Person) && a.EntityId == person.Id.ToString())
            .ToListAsync();

        var entry = Assert.Single(auditRows);
        Assert.Equal(AuditAction.Insert, entry.Action);
        Assert.Null(entry.BeforeJson);
        Assert.Contains("Ada", entry.AfterJson);
    }

    [Fact]
    public async Task Updating_an_audited_entity_records_both_before_and_after_values()
    {
        await using var seedContext = CreateContext();
        var person = new Person
        {
            Id = Guid.NewGuid(),
            FirstName = "Grace",
            LastName = "Hopper",
            DateOfBirth = new DateOnly(1985, 1, 1),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        seedContext.People.Add(person);
        await seedContext.SaveChangesAsync();

        await using var updateContext = CreateContext();
        var tracked = await updateContext.People.SingleAsync(p => p.Id == person.Id);
        tracked.LastName = "Murray";
        await updateContext.SaveChangesAsync();

        var updateAudit = await updateContext.AuditLog
            .Where(a => a.EntityType == nameof(Person) && a.EntityId == person.Id.ToString() && a.Action == AuditAction.Update)
            .SingleAsync();

        Assert.Contains("Hopper", updateAudit.BeforeJson);
        Assert.Contains("Murray", updateAudit.AfterJson);
    }

    [Fact]
    public async Task Deleting_an_audited_entity_records_the_before_value_and_no_after_value()
    {
        await using var seedContext = CreateContext();
        var person = new Person
        {
            Id = Guid.NewGuid(),
            FirstName = "Katherine",
            LastName = "Johnson",
            DateOfBirth = new DateOnly(1980, 1, 1),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        seedContext.People.Add(person);
        await seedContext.SaveChangesAsync();

        await using var deleteContext = CreateContext();
        var tracked = await deleteContext.People.SingleAsync(p => p.Id == person.Id);
        deleteContext.People.Remove(tracked);
        await deleteContext.SaveChangesAsync();

        var deleteAudit = await deleteContext.AuditLog
            .Where(a => a.EntityType == nameof(Person) && a.EntityId == person.Id.ToString() && a.Action == AuditAction.Delete)
            .SingleAsync();

        Assert.Contains("Johnson", deleteAudit.BeforeJson);
        Assert.Null(deleteAudit.AfterJson);
    }

    [Fact]
    public async Task Audit_row_attributes_the_change_to_the_current_request_context()
    {
        var actorId = Guid.NewGuid();
        var requestContext = new FixedRequestContext(actorId, "corr-123");
        await using var context = CreateContext(requestContext);

        var person = new Person
        {
            Id = Guid.NewGuid(),
            FirstName = "Rear",
            LastName = "Admiral",
            DateOfBirth = new DateOnly(1970, 1, 1),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        context.People.Add(person);
        await context.SaveChangesAsync();

        var entry = await context.AuditLog
            .Where(a => a.EntityType == nameof(Person) && a.EntityId == person.Id.ToString())
            .SingleAsync();

        Assert.Equal(actorId, entry.ActorPersonId);
        Assert.Equal("corr-123", entry.CorrelationId);
    }

    [Fact]
    public async Task Saving_an_entity_not_on_the_audit_allowlist_writes_no_audit_row()
    {
        await using var context = CreateContext();

        context.BodyRegions.Add(new BodyRegion
        {
            Code = "test_region_" + Guid.NewGuid(),
            DisplayName = "Test Region",
            RegionGroup = "Test",
        });
        await context.SaveChangesAsync();

        var auditCount = await context.AuditLog.CountAsync();
        Assert.Equal(0, auditCount);
    }

    private sealed class FixedRequestContext(Guid? actorPersonId, string? correlationId) : ICurrentRequestContext
    {
        public Guid? ActorPersonId => actorPersonId;
        public string? CorrelationId => correlationId;
    }
}
