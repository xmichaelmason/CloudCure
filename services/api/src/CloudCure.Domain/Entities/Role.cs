using CloudCure.Domain.Enums;

namespace CloudCure.Domain.Entities;

/// <summary>Fixed lookup table backing <see cref="RoleName"/> — replaces the old app's free-text `RoleName` column.</summary>
public class Role
{
    public RoleName Id { get; set; }
    public required string Name { get; set; }

    public ICollection<PersonRole> PersonRoles { get; set; } = [];
}
