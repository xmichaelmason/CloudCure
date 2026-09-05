namespace CloudCure.Domain.Entities;

public class PatientMedication
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public int? ReferenceId { get; set; }
    public string? FreeTextName { get; set; }
    public DateTimeOffset RecordedAt { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }

    public Patient Patient { get; set; } = null!;
    public MedicationReference? Reference { get; set; }
}
