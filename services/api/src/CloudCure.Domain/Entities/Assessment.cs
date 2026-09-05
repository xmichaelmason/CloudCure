namespace CloudCure.Domain.Entities;

/// <summary>1:1 with an encounter. Pain scale is additionally CHECK-constrained (0-10) at the DB level.</summary>
public class Assessment
{
    public int Id { get; set; }
    public int EncounterId { get; set; }
    public required string ChiefComplaint { get; set; }
    public required string HistoryOfPresentIllness { get; set; }
    public int PainScale { get; set; }

    public Encounter Encounter { get; set; } = null!;
    public ICollection<AssessmentPainPoint> PainPoints { get; set; } = [];
}
