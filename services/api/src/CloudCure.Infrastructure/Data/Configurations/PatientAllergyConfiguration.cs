using CloudCure.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudCure.Infrastructure.Data.Configurations;

public class PatientAllergyConfiguration : IEntityTypeConfiguration<PatientAllergy>
{
    public void Configure(EntityTypeBuilder<PatientAllergy> builder)
    {
        builder.ToTable("patient_allergies", tb => tb.HasCheckConstraint(
            "ck_patient_allergies_reference_or_free_text",
            "reference_id IS NOT NULL OR free_text_name IS NOT NULL"));
        builder.HasKey(a => a.Id);

        builder.Property(a => a.FreeTextName).HasMaxLength(200);

        builder.HasOne(a => a.Patient)
            .WithMany(p => p.Allergies)
            .HasForeignKey(a => a.PatientId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Reference)
            .WithMany(r => r.PatientAllergies)
            .HasForeignKey(a => a.ReferenceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
