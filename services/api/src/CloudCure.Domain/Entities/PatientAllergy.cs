namespace CloudCure.Domain.Entities;

/// <summary>
/// Links a patient to either a canonical <see cref="AllergyReference"/> or a free-text name
/// (DB CHECK-constrained so exactly one is required) — also adds the recorded-date/active-flag
/// the old app lacked, so current vs. historical items are distinguishable.
/// </summary>
public class PatientAllergy
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public int? ReferenceId { get; set; }
    public string? FreeTextName { get; set; }
    public DateTimeOffset RecordedAt { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }

    public Patient Patient { get; set; } = null!;
    public AllergyReference? Reference { get; set; }
}
