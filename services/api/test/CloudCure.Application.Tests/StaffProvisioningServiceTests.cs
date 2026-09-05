using CloudCure.Application.Identity;
using CloudCure.Domain.Enums;
using CloudCure.Infrastructure.Audit;
using CloudCure.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace CloudCure.Application.Tests;

public class StaffProvisioningServiceTests : IAsyncLifetime
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

    private async Task<Guid> ProvisionPendingPersonAsync(CloudCureDbContext context, string auth0Subject, string firstName, string lastName)
    {
        var identity = new IdentityResolutionService(context);
        var result = await identity.ResolveOrProvisionAsync(auth0Subject, $"{firstName}@example.com", firstName, lastName);
        return result.PersonId;
    }

    [Fact]
    public async Task RegisterAsync_creates_a_staff_member_profile_for_an_existing_pending_person()
    {
        await using var context = CreateContext();
        var personId = await ProvisionPendingPersonAsync(context, "auth0|nurse-1", "Florence", "Nightingale");
        var service = new StaffProvisioningService(context);

        await service.RegisterAsync(personId, "florence@clinic.example", "General Practice", new DateOnly(2020, 1, 1), "204", "MD");

        var staffMember = await context.StaffMembers.SingleAsync(s => s.PersonId == personId);
        Assert.Equal("florence@clinic.example", staffMember.WorkEmail);
        Assert.Equal("General Practice", staffMember.Specialization);
    }

    [Fact]
    public async Task ListPendingApprovalsAsync_returns_only_people_awaiting_approval()
    {
        await using var context = CreateContext();
        var pendingId = await ProvisionPendingPersonAsync(context, "auth0|pending-1", "Marie", "Curie");
        var approvedId = await ProvisionPendingPersonAsync(context, "auth0|approved-1", "Rosalind", "Franklin");
        var adminId = await ProvisionPendingPersonAsync(context, "auth0|admin-3", "Ada", "Yonath");

        var service = new StaffProvisioningService(context);
        await service.ApproveAsync(approvedId, adminId, RoleName.Nurse);

        var pending = await service.ListPendingApprovalsAsync();

        var pendingIds = pending.Select(p => p.PersonId).ToList();
        Assert.Contains(pendingId, pendingIds);
        Assert.DoesNotContain(approvedId, pendingIds);
    }

    [Fact]
    public async Task ApproveAsync_activates_the_requested_role_and_records_who_granted_it()
    {
        await using var context = CreateContext();
        var applicantId = await ProvisionPendingPersonAsync(context, "auth0|applicant-1", "Chien-Shiung", "Wu");
        var adminId = await ProvisionPendingPersonAsync(context, "auth0|admin-1", "Ada", "Yonath");
        var service = new StaffProvisioningService(context);

        await service.ApproveAsync(applicantId, adminId, RoleName.Doctor);

        var role = await context.PersonRoles.SingleAsync(pr => pr.PersonId == applicantId);
        Assert.Equal(RoleName.Doctor, role.RoleId);
        Assert.Equal(PersonRoleStatus.Active, role.Status);
        Assert.Equal(adminId, role.GrantedByPersonId);
    }

    [Fact]
    public async Task ApproveAsync_throws_when_the_person_has_no_pending_request()
    {
        await using var context = CreateContext();
        var adminId = await ProvisionPendingPersonAsync(context, "auth0|admin-2", "Ada", "Yonath");
        var service = new StaffProvisioningService(context);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ApproveAsync(Guid.NewGuid(), adminId, RoleName.Nurse));
    }
}
