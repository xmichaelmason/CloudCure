namespace CloudCure.Domain.Entities;

/// <summary>
/// Shared demographic base for every human the system knows about — patient or staff.
/// Replaces the old app's single `Users` table that conflated both roles.
/// </summary>
public class Person
{
    public Guid Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    // Nullable: identity resolution creates a bare Person from Auth0 claims, which never
    // include date of birth — it's filled in afterward via the registration/intake form.
    public DateOnly? DateOfBirth { get; set; }
    public string? PhoneE164 { get; set; }
    public string? Email { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Address? Address { get; set; }
    public Patient? Patient { get; set; }
    public StaffMember? StaffMember { get; set; }
    public ICollection<AuthIdentity> AuthIdentities { get; set; } = [];
    public ICollection<PersonRole> PersonRoles { get; set; } = [];
}
