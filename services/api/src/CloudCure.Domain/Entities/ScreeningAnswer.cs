namespace CloudCure.Domain.Entities;

public class ScreeningAnswer
{
    public int Id { get; set; }
    public int ScreeningId { get; set; }
    public int ScreeningQuestionId { get; set; }
    public bool? AnswerBool { get; set; }
    public string? AnswerText { get; set; }
    public decimal? AnswerNumber { get; set; }

    public Screening Screening { get; set; } = null!;
    public ScreeningQuestion ScreeningQuestion { get; set; } = null!;
}
