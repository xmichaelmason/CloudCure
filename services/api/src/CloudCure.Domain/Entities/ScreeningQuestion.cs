using CloudCure.Domain.Enums;

namespace CloudCure.Domain.Entities;

public class ScreeningQuestion
{
    public int Id { get; set; }
    public int ScreeningTemplateId { get; set; }
    public int DisplayOrder { get; set; }
    public required string QuestionText { get; set; }
    public ScreeningAnswerType AnswerType { get; set; }

    public ScreeningTemplate ScreeningTemplate { get; set; } = null!;
    public ICollection<ScreeningAnswer> Answers { get; set; } = [];
}
