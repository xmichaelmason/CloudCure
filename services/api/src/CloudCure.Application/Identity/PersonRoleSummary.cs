using CloudCure.Domain.Entities;
using CloudCure.Domain.Enums;

namespace CloudCure.Application.Identity;

public record PersonRoleSummary(IReadOnlyList<string> Roles, string Status)
{
    public static PersonRoleSummary From(IEnumerable<PersonRole> personRoles)
    {
        var roles = personRoles as ICollection<PersonRole> ?? personRoles.ToList();

        var activeRoles = roles
            .Where(pr => pr.Status == PersonRoleStatus.Active)
            .Select(pr => pr.RoleId.ToString())
            .ToList();

        var status = activeRoles.Count > 0
            ? "Active"
            : roles.Any(pr => pr.Status == PersonRoleStatus.Pending)
                ? "Pending"
                : "Revoked";

        return new PersonRoleSummary(activeRoles, status);
    }
}
