using CloudCure.Domain.Entities;
using CloudCure.Domain.Enums;
using CloudCure.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CloudCure.Application.Identity;

/// <summary>
/// Find-or-create identity provisioning for a Auth0-authenticated caller. Role is never
/// accepted from the caller — a first-time login always lands as
/// <see cref="RoleName.Pending"/>/<see cref="PersonRoleStatus.Pending"/>; only
/// <see cref="StaffProvisioningService.ApproveAsync"/> (admin-only) can change that. This is
/// the direct fix for the old app's self-assignable-role registration bug.
/// </summary>
public class IdentityResolutionService(CloudCureDbContext db)
{
    public async Task<IdentityResolution> ResolveOrProvisionAsync(
        string auth0Subject, string? email, string firstName, string lastName, CancellationToken cancellationToken = default)
    {
        var authIdentity = await db.AuthIdentities
            .Include(a => a.Person)
            .ThenInclude(p => p.PersonRoles)
            .FirstOrDefaultAsync(a => a.Auth0Subject == auth0Subject, cancellationToken);

        if (authIdentity is not null)
        {
            return ToResolution(authIdentity.PersonId, authIdentity.Person.PersonRoles);
        }

        var person = new Person
        {
            Id = Guid.NewGuid(),
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        db.People.Add(person);
        db.AuthIdentities.Add(new AuthIdentity { PersonId = person.Id, Auth0Subject = auth0Subject });
        var pendingRole = new PersonRole
        {
            PersonId = person.Id,
            RoleId = RoleName.Pending,
            Status = PersonRoleStatus.Pending,
            GrantedAt = DateTimeOffset.UtcNow,
        };
        db.PersonRoles.Add(pendingRole);

        await db.SaveChangesAsync(cancellationToken);

        return ToResolution(person.Id, [pendingRole]);
    }

    private static IdentityResolution ToResolution(Guid personId, IEnumerable<PersonRole> personRoles)
    {
        var summary = PersonRoleSummary.From(personRoles);
        return new IdentityResolution(personId, summary.Roles, summary.Status);
    }
}

public record IdentityResolution(Guid PersonId, IReadOnlyList<string> Roles, string Status);
