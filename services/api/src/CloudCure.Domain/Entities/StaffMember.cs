namespace CloudCure.Domain.Entities;

public class StaffMember
{
    public int Id { get; set; }
    public Guid PersonId { get; set; }
    public required string WorkEmail { get; set; }
    public required string Specialization { get; set; }
    public DateOnly StartDate { get; set; }
    public string? RoomNumber { get; set; }
    public required string EducationDegree { get; set; }

    public Person Person { get; set; } = null!;
    public ICollection<Encounter> AttendedEncounters { get; set; } = [];
}
