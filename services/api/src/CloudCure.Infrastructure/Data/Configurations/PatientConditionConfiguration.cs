using CloudCure.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudCure.Infrastructure.Data.Configurations;

public class PatientConditionConfiguration : IEntityTypeConfiguration<PatientCondition>
{
    public void Configure(EntityTypeBuilder<PatientCondition> builder)
    {
        builder.ToTable("patient_conditions", tb => tb.HasCheckConstraint(
            "ck_patient_conditions_reference_or_free_text",
            "reference_id IS NOT NULL OR free_text_name IS NOT NULL"));
        builder.HasKey(c => c.Id);

        builder.Property(c => c.FreeTextName).HasMaxLength(200);

        builder.HasOne(c => c.Patient)
            .WithMany(p => p.Conditions)
            .HasForeignKey(c => c.PatientId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.Reference)
            .WithMany(r => r.PatientConditions)
            .HasForeignKey(c => c.ReferenceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
