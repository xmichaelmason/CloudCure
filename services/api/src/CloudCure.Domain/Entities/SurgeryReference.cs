namespace CloudCure.Domain.Entities;

public class SurgeryReference
{
    public int Id { get; set; }
    public required string CanonicalName { get; set; }
    public string[]? Aliases { get; set; }

    public ICollection<PatientSurgery> PatientSurgeries { get; set; } = [];
}
