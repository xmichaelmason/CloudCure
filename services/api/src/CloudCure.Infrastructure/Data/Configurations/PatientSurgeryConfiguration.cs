using CloudCure.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudCure.Infrastructure.Data.Configurations;

public class PatientSurgeryConfiguration : IEntityTypeConfiguration<PatientSurgery>
{
    public void Configure(EntityTypeBuilder<PatientSurgery> builder)
    {
        builder.ToTable("patient_surgeries", tb => tb.HasCheckConstraint(
            "ck_patient_surgeries_reference_or_free_text",
            "reference_id IS NOT NULL OR free_text_name IS NOT NULL"));
        builder.HasKey(s => s.Id);

        builder.Property(s => s.FreeTextName).HasMaxLength(200);

        builder.HasOne(s => s.Patient)
            .WithMany(p => p.Surgeries)
            .HasForeignKey(s => s.PatientId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Reference)
            .WithMany(r => r.PatientSurgeries)
            .HasForeignKey(s => s.ReferenceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
