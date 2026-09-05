namespace CloudCure.Api.Contracts;

public record ScreeningQuestionResponse(int Id, int DisplayOrder, string QuestionText, string AnswerType);

public record ScreeningTemplateResponse(int Id, string Code, string Title, IReadOnlyList<ScreeningQuestionResponse> Questions);

public record ScreeningAnswerApiInput(int QuestionId, bool? AnswerBool, string? AnswerText, decimal? AnswerNumber);

public record SubmitScreeningRequest(string TemplateCode, int? EncounterId, IReadOnlyList<ScreeningAnswerApiInput> Answers);

public record ScreeningCreatedResponse(int ScreeningId);
