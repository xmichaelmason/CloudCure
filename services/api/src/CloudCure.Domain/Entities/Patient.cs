namespace CloudCure.Domain.Entities;

public class Patient
{
    public int Id { get; set; }
    public Guid PersonId { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhoneE164 { get; set; }

    public Person Person { get; set; } = null!;
    public ICollection<Encounter> Encounters { get; set; } = [];
    public ICollection<PatientAllergy> Allergies { get; set; } = [];
    public ICollection<PatientCondition> Conditions { get; set; } = [];
    public ICollection<PatientMedication> Medications { get; set; } = [];
    public ICollection<PatientSurgery> Surgeries { get; set; } = [];
    public ICollection<Screening> Screenings { get; set; } = [];
}
