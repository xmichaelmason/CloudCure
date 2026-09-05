using CloudCure.Domain.Entities;
using CloudCure.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudCure.Infrastructure.Data.Configurations;

public class ScreeningQuestionConfiguration : IEntityTypeConfiguration<ScreeningQuestion>
{
    public void Configure(EntityTypeBuilder<ScreeningQuestion> builder)
    {
        builder.ToTable("screening_questions");
        builder.HasKey(q => q.Id);

        builder.Property(q => q.QuestionText).IsRequired();

        builder.HasOne(q => q.ScreeningTemplate)
            .WithMany(t => t.Questions)
            .HasForeignKey(q => q.ScreeningTemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        // Standard COVID-19 screening questions, seeded as data rather than hardcoded columns
        // (the old app's `question1..question5` columns). Note: the old app's exact question
        // wording could not be fully recovered — it was hand-duplicated and inconsistent
        // across 3 different Angular templates, which is itself the bug this design fixes —
        // so these are reasonable standard-screening equivalents, not a byte-for-byte port.
        builder.HasData(
            new ScreeningQuestion
            {
                Id = 1,
                ScreeningTemplateId = 1,
                DisplayOrder = 1,
                QuestionText = "Are you currently experiencing a fever, cough, or shortness of breath?",
                AnswerType = ScreeningAnswerType.YesNo,
            },
            new ScreeningQuestion
            {
                Id = 2,
                ScreeningTemplateId = 1,
                DisplayOrder = 2,
                QuestionText = "Are you currently isolating or quarantining due to a COVID-19 exposure or diagnosis?",
                AnswerType = ScreeningAnswerType.YesNo,
            },
            new ScreeningQuestion
            {
                Id = 3,
                ScreeningTemplateId = 1,
                DisplayOrder = 3,
                QuestionText = "Has anyone in your household experienced any COVID-19 symptoms in the last 14 days?",
                AnswerType = ScreeningAnswerType.YesNo,
            },
            new ScreeningQuestion
            {
                Id = 4,
                ScreeningTemplateId = 1,
                DisplayOrder = 4,
                QuestionText = "Have you tested positive for COVID-19 in the last 10 days?",
                AnswerType = ScreeningAnswerType.YesNo,
            },
            new ScreeningQuestion
            {
                Id = 5,
                ScreeningTemplateId = 1,
                DisplayOrder = 5,
                QuestionText = "Have you traveled outside the country in the last 14 days?",
                AnswerType = ScreeningAnswerType.YesNo,
            });
    }
}
