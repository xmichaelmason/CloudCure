using CloudCure.Domain.Enums;

namespace CloudCure.Domain.Entities;

/// <summary>
/// The visit/appointment record. Replaces the old app's `Diagnosis` entity, which conflated
/// the visit itself with its clinical outcome — see <see cref="Diagnosis"/> for the outcome.
/// </summary>
public class Encounter
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public int? AttendingStaffMemberId { get; set; }
    public DateTimeOffset? ScheduledAt { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public EncounterStage Stage { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Patient Patient { get; set; } = null!;
    public StaffMember? AttendingStaffMember { get; set; }
    public Vitals? Vitals { get; set; }
    public Assessment? Assessment { get; set; }
    public Diagnosis? Diagnosis { get; set; }
    public ICollection<Screening> Screenings { get; set; } = [];
}
