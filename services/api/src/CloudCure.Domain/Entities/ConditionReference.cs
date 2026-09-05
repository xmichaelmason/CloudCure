namespace CloudCure.Domain.Entities;

public class ConditionReference
{
    public int Id { get; set; }
    public required string CanonicalName { get; set; }
    public string[]? Aliases { get; set; }

    public ICollection<PatientCondition> PatientConditions { get; set; } = [];
}
