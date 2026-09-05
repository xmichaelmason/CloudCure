using CloudCure.Domain.Enums;

namespace CloudCure.Domain.Entities;

/// <summary>
/// Many-to-many grant of a staff-privilege role to a person, with an audit trail of who
/// granted it and when — enables the admin-approval provisioning flow instead of the old
/// app's client-trusted, self-assignable role.
/// </summary>
public class PersonRole
{
    public Guid PersonId { get; set; }
    public RoleName RoleId { get; set; }
    public PersonRoleStatus Status { get; set; }
    public Guid? GrantedByPersonId { get; set; }
    public DateTimeOffset GrantedAt { get; set; }

    public Person Person { get; set; } = null!;
    public Role Role { get; set; } = null!;
    public Person? GrantedByPerson { get; set; }
}
