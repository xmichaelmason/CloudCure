using CloudCure.Domain.Entities;
using CloudCure.Domain.Enums;
using CloudCure.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CloudCure.Application.Identity;

/// <summary>
/// Staff self-registration (profile details only — never a role) and the admin-only
/// approval step that is the sole path to granting clinical privileges.
/// </summary>
public class StaffProvisioningService(CloudCureDbContext db)
{
    public async Task RegisterAsync(
        Guid personId,
        string workEmail,
        string specialization,
        DateOnly startDate,
        string? roomNumber,
        string educationDegree,
        CancellationToken cancellationToken = default)
    {
        var alreadyRegistered = await db.StaffMembers.AnyAsync(s => s.PersonId == personId, cancellationToken);
        if (alreadyRegistered)
        {
            throw new InvalidOperationException($"Person {personId} is already registered as a staff member.");
        }

        db.StaffMembers.Add(new StaffMember
        {
            PersonId = personId,
            WorkEmail = workEmail,
            Specialization = specialization,
            StartDate = startDate,
            RoomNumber = roomNumber,
            EducationDegree = educationDegree,
        });

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PendingApproval>> ListPendingApprovalsAsync(CancellationToken cancellationToken = default)
    {
        return await db.PersonRoles
            .Where(pr => pr.Status == PersonRoleStatus.Pending)
            .Select(pr => new PendingApproval(pr.PersonId, pr.Person.FirstName, pr.Person.LastName, pr.Person.Email, pr.GrantedAt))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// The only path that grants a staff privilege — always an explicit admin action naming
    /// the person and the role, never a value the applicant supplied about themselves.
    /// </summary>
    public async Task ApproveAsync(Guid personId, Guid approvedByPersonId, RoleName role, CancellationToken cancellationToken = default)
    {
        var pendingRole = await db.PersonRoles.SingleOrDefaultAsync(
            pr => pr.PersonId == personId && pr.Status == PersonRoleStatus.Pending, cancellationToken)
            ?? throw new InvalidOperationException($"Person {personId} has no pending approval request.");

        // RoleId is part of person_roles' composite key, so granting a *different* role than
        // was pending is a new row, not an in-place update of the existing one.
        db.PersonRoles.Remove(pendingRole);
        db.PersonRoles.Add(new PersonRole
        {
            PersonId = personId,
            RoleId = role,
            Status = PersonRoleStatus.Active,
            GrantedByPersonId = approvedByPersonId,
            GrantedAt = DateTimeOffset.UtcNow,
        });

        await db.SaveChangesAsync(cancellationToken);
    }
}

public record PendingApproval(Guid PersonId, string FirstName, string LastName, string? Email, DateTimeOffset RequestedAt);
