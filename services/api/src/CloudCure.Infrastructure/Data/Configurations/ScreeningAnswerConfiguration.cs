using CloudCure.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudCure.Infrastructure.Data.Configurations;

public class ScreeningAnswerConfiguration : IEntityTypeConfiguration<ScreeningAnswer>
{
    public void Configure(EntityTypeBuilder<ScreeningAnswer> builder)
    {
        builder.ToTable("screening_answers");
        builder.HasKey(a => a.Id);

        builder.HasIndex(a => new { a.ScreeningId, a.ScreeningQuestionId }).IsUnique();

        builder.HasOne(a => a.Screening)
            .WithMany(s => s.Answers)
            .HasForeignKey(a => a.ScreeningId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.ScreeningQuestion)
            .WithMany(q => q.Answers)
            .HasForeignKey(a => a.ScreeningQuestionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
