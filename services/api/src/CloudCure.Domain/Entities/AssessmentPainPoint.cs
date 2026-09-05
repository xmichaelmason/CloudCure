namespace CloudCure.Domain.Entities;

/// <summary>Normalized replacement for the old app's comma-separated `PainAssessment` string column.</summary>
public class AssessmentPainPoint
{
    public int AssessmentId { get; set; }
    public int BodyRegionId { get; set; }

    public Assessment Assessment { get; set; } = null!;
    public BodyRegion BodyRegion { get; set; } = null!;
}
