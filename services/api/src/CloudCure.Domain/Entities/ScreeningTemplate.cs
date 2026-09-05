namespace CloudCure.Domain.Entities;

/// <summary>
/// A versioned, named questionnaire (e.g. "covid19_v1"). Replaces the old app's hardcoded
/// `question1..question5` columns with a data-driven model — adding a question, or a new
/// screening type entirely, is a data change, not a schema migration.
/// </summary>
public class ScreeningTemplate
{
    public int Id { get; set; }
    public required string Code { get; set; }
    public required string Title { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<ScreeningQuestion> Questions { get; set; } = [];
    public ICollection<Screening> Screenings { get; set; } = [];
}
