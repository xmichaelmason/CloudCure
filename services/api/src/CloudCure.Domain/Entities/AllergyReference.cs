namespace CloudCure.Domain.Entities;

/// <summary>
/// Canonical allergy name lookup — so "Penicillin"/"penicillin"/"PCN" resolve to one concept
/// instead of the old app's inconsistent free-text-only storage.
/// </summary>
public class AllergyReference
{
    public int Id { get; set; }
    public required string CanonicalName { get; set; }
    public string[]? Aliases { get; set; }

    public ICollection<PatientAllergy> PatientAllergies { get; set; } = [];
}
