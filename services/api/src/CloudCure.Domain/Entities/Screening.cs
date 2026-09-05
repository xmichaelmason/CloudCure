namespace CloudCure.Domain.Entities;

/// <summary>One completed submission of a <see cref="ScreeningTemplate"/> for a patient.</summary>
public class Screening
{
    public int Id { get; set; }
    public int ScreeningTemplateId { get; set; }
    public int PatientId { get; set; }
    public int? EncounterId { get; set; }
    public DateTimeOffset CompletedAt { get; set; }
    public Guid CompletedByPersonId { get; set; }

    public ScreeningTemplate ScreeningTemplate { get; set; } = null!;
    public Patient Patient { get; set; } = null!;
    public Encounter? Encounter { get; set; }
    public Person CompletedByPerson { get; set; } = null!;
    public ICollection<ScreeningAnswer> Answers { get; set; } = [];
}
