namespace CloudCure.Domain.Entities;

/// <summary>Purely the clinical outcome of an encounter — 1:1, DB-enforced unique on <see cref="EncounterId"/>.</summary>
public class Diagnosis
{
    public int Id { get; set; }
    public int EncounterId { get; set; }
    public string? DoctorDiagnosisText { get; set; }
    public string? RecommendedTreatment { get; set; }
    public int? FinalizedByStaffMemberId { get; set; }
    public DateTimeOffset? FinalizedAt { get; set; }

    public Encounter Encounter { get; set; } = null!;
    public StaffMember? FinalizedByStaffMember { get; set; }
}
