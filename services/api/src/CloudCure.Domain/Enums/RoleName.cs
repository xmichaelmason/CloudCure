namespace CloudCure.Domain.Enums;

/// <summary>Fixed staff-privilege roles. Backed by the `roles` lookup table (id matches this enum's numeric value).</summary>
public enum RoleName
{
    Pending = 1,
    Nurse = 2,
    Doctor = 3,
    Admin = 4,
}
