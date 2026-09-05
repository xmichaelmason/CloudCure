namespace CloudCure.Domain.Entities;

public class MedicationReference
{
    public int Id { get; set; }
    public required string CanonicalName { get; set; }
    public string[]? Aliases { get; set; }

    public ICollection<PatientMedication> PatientMedications { get; set; } = [];
}
