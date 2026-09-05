using CloudCure.Domain.Entities;
using CloudCure.Domain.Enums;
using CloudCure.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CloudCure.Application.Screenings;

/// <summary>
/// Reads and submits screening questionnaires entirely from `screening_questions` data —
/// replaces the old app's hardcoded `question1..question5` columns, where adding a question
/// meant a schema migration and where question text had drifted out of sync across three
/// separately hand-copied Angular templates.
/// </summary>
public class ScreeningService(CloudCureDbContext db)
{
    public async Task<ScreeningTemplateInfo?> GetTemplateAsync(string code, CancellationToken cancellationToken = default)
    {
        var template = await db.ScreeningTemplates
            .Include(t => t.Questions.OrderBy(q => q.DisplayOrder))
            .FirstOrDefaultAsync(t => t.Code == code && t.IsActive, cancellationToken);

        if (template is null)
        {
            return null;
        }

        var questions = template.Questions
            .OrderBy(q => q.DisplayOrder)
            .Select(q => new ScreeningQuestionInfo(q.Id, q.DisplayOrder, q.QuestionText, q.AnswerType))
            .ToList();

        return new ScreeningTemplateInfo(template.Id, template.Code, template.Title, questions);
    }

    public async Task<int> SubmitAsync(
        int patientId,
        string templateCode,
        Guid completedByPersonId,
        int? encounterId,
        IReadOnlyList<ScreeningAnswerInput> answers,
        CancellationToken cancellationToken = default)
    {
        var template = await db.ScreeningTemplates.FirstOrDefaultAsync(t => t.Code == templateCode, cancellationToken)
            ?? throw new InvalidOperationException($"Unknown screening template '{templateCode}'.");

        var screening = new Screening
        {
            ScreeningTemplateId = template.Id,
            PatientId = patientId,
            EncounterId = encounterId,
            CompletedAt = DateTimeOffset.UtcNow,
            CompletedByPersonId = completedByPersonId,
        };
        db.Screenings.Add(screening);

        foreach (var answer in answers)
        {
            db.ScreeningAnswers.Add(new ScreeningAnswer
            {
                Screening = screening,
                ScreeningQuestionId = answer.QuestionId,
                AnswerBool = answer.AnswerBool,
                AnswerText = answer.AnswerText,
                AnswerNumber = answer.AnswerNumber,
            });
        }

        await db.SaveChangesAsync(cancellationToken);

        return screening.Id;
    }
}

public record ScreeningQuestionInfo(int Id, int DisplayOrder, string QuestionText, ScreeningAnswerType AnswerType);

public record ScreeningTemplateInfo(int Id, string Code, string Title, IReadOnlyList<ScreeningQuestionInfo> Questions);

public record ScreeningAnswerInput(int QuestionId, bool? AnswerBool, string? AnswerText, decimal? AnswerNumber);
