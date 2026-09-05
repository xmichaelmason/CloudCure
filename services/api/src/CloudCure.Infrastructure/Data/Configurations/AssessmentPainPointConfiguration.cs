using CloudCure.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudCure.Infrastructure.Data.Configurations;

public class AssessmentPainPointConfiguration : IEntityTypeConfiguration<AssessmentPainPoint>
{
    public void Configure(EntityTypeBuilder<AssessmentPainPoint> builder)
    {
        builder.ToTable("assessment_pain_points");
        builder.HasKey(p => new { p.AssessmentId, p.BodyRegionId });

        builder.HasOne(p => p.Assessment)
            .WithMany(a => a.PainPoints)
            .HasForeignKey(p => p.AssessmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.BodyRegion)
            .WithMany(b => b.AssessmentPainPoints)
            .HasForeignKey(p => p.BodyRegionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
