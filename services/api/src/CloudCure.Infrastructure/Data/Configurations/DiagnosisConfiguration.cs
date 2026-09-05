using CloudCure.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudCure.Infrastructure.Data.Configurations;

public class DiagnosisConfiguration : IEntityTypeConfiguration<Diagnosis>
{
    public void Configure(EntityTypeBuilder<Diagnosis> builder)
    {
        builder.ToTable("diagnoses");
        builder.HasKey(d => d.Id);

        // HasForeignKey<Diagnosis> on the required 1:1 side makes EF add a unique index on
        // encounter_id automatically — DB-enforced true 1:1, which the old app never had.
        builder.HasOne(d => d.Encounter)
            .WithOne(e => e.Diagnosis)
            .HasForeignKey<Diagnosis>(d => d.EncounterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(d => d.FinalizedByStaffMember)
            .WithMany()
            .HasForeignKey(d => d.FinalizedByStaffMemberId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
