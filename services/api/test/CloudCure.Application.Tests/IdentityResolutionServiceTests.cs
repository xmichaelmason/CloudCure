using CloudCure.Application.Identity;
using CloudCure.Domain.Entities;
using CloudCure.Domain.Enums;
using CloudCure.Infrastructure.Audit;
using CloudCure.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace CloudCure.Application.Tests;

/// <summary>
/// Regression coverage for the old app's core security gap: a new user could self-assign any
/// role (including "Doctor") at registration. Here, role is never read from the caller —
/// first login always lands as Pending, and only an explicit admin approval (tested in
/// StaffProvisioningServiceTests) can change that.
/// </summary>
public class IdentityResolutionServiceTests : IAsyncLifetime
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

    private CloudCureDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<CloudCureDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .UseSnakeCaseNamingConvention()
            .Options;

        return new CloudCureDbContext(options, NullCurrentRequestContext.Instance);
    }

    [Fact]
    public async Task First_login_for_a_new_auth0_subject_provisions_a_pending_person_with_no_roles()
    {
        await using var context = CreateContext();
        var service = new IdentityResolutionService(context);

        var result = await service.ResolveOrProvisionAsync("auth0|new-user", "new@example.com", "Ada", "Lovelace");

        Assert.Equal("Pending", result.Status);
        Assert.Empty(result.Roles);
        Assert.NotEqual(Guid.Empty, result.PersonId);

        var person = await context.People.SingleAsync(p => p.Id == result.PersonId);
        Assert.Equal("Ada", person.FirstName);
        Assert.Equal("Lovelace", person.LastName);

        var personRole = await context.PersonRoles.SingleAsync(pr => pr.PersonId == result.PersonId);
        Assert.Equal(RoleName.Pending, personRole.RoleId);
        Assert.Equal(PersonRoleStatus.Pending, personRole.Status);
    }

    [Fact]
    public async Task Repeated_login_for_the_same_auth0_subject_does_not_create_a_second_person()
    {
        await using var context = CreateContext();
        var service = new IdentityResolutionService(context);

        var first = await service.ResolveOrProvisionAsync("auth0|repeat-user", "repeat@example.com", "Grace", "Hopper");
        var second = await service.ResolveOrProvisionAsync("auth0|repeat-user", "repeat@example.com", "Grace", "Hopper");

        Assert.Equal(first.PersonId, second.PersonId);
        Assert.Equal(1, await context.People.CountAsync(p => p.Id == first.PersonId));
        Assert.Equal(1, await context.AuthIdentities.CountAsync(a => a.Auth0Subject == "auth0|repeat-user"));
    }

    [Fact]
    public async Task Resolving_an_already_approved_person_reports_their_active_role()
    {
        await using var context = CreateContext();
        var service = new IdentityResolutionService(context);

        var provisioned = await service.ResolveOrProvisionAsync("auth0|doctor-user", "doc@example.com", "Katherine", "Johnson");

        // RoleId is part of person_roles' composite key, so becoming a Doctor replaces the
        // pending row rather than mutating it in place (see StaffProvisioningService.ApproveAsync).
        var pendingRole = await context.PersonRoles.SingleAsync(pr => pr.PersonId == provisioned.PersonId);
        context.PersonRoles.Remove(pendingRole);
        context.PersonRoles.Add(new PersonRole
        {
            PersonId = provisioned.PersonId,
            RoleId = RoleName.Doctor,
            Status = PersonRoleStatus.Active,
            GrantedAt = DateTimeOffset.UtcNow,
        });
        await context.SaveChangesAsync();

        var resolved = await service.ResolveOrProvisionAsync("auth0|doctor-user", "doc@example.com", "Katherine", "Johnson");

        Assert.Equal("Active", resolved.Status);
        Assert.Equal(["Doctor"], resolved.Roles);
    }
}
