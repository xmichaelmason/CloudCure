using CloudCure.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudCure.Infrastructure.Data.Configurations;

public class AssessmentConfiguration : IEntityTypeConfiguration<Assessment>
{
    public void Configure(EntityTypeBuilder<Assessment> builder)
    {
        builder.ToTable("assessments", tb => tb.HasCheckConstraint("ck_assessments_pain_scale", "pain_scale BETWEEN 0 AND 10"));
        builder.HasKey(a => a.Id);

        builder.Property(a => a.ChiefComplaint).IsRequired();
        builder.Property(a => a.HistoryOfPresentIllness).IsRequired();

        builder.HasOne(a => a.Encounter)
            .WithOne(e => e.Assessment)
            .HasForeignKey<Assessment>(a => a.EncounterId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
