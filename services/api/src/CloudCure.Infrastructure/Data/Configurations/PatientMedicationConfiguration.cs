using CloudCure.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudCure.Infrastructure.Data.Configurations;

public class PatientMedicationConfiguration : IEntityTypeConfiguration<PatientMedication>
{
    public void Configure(EntityTypeBuilder<PatientMedication> builder)
    {
        builder.ToTable("patient_medications", tb => tb.HasCheckConstraint(
            "ck_patient_medications_reference_or_free_text",
            "reference_id IS NOT NULL OR free_text_name IS NOT NULL"));
        builder.HasKey(m => m.Id);

        builder.Property(m => m.FreeTextName).HasMaxLength(200);

        builder.HasOne(m => m.Patient)
            .WithMany(p => p.Medications)
            .HasForeignKey(m => m.PatientId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.Reference)
            .WithMany(r => r.PatientMedications)
            .HasForeignKey(m => m.ReferenceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
