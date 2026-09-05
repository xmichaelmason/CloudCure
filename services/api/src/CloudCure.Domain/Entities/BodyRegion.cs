namespace CloudCure.Domain.Entities;

/// <summary>
/// Categorized lookup of selectable body regions — replaces the old app's 825-line
/// hardcoded pixel-coordinate body-clicker component with a normalized, data-driven list.
/// </summary>
public class BodyRegion
{
    public int Id { get; set; }
    public required string Code { get; set; }
    public required string DisplayName { get; set; }
    public required string RegionGroup { get; set; }

    public ICollection<AssessmentPainPoint> AssessmentPainPoints { get; set; } = [];
}
