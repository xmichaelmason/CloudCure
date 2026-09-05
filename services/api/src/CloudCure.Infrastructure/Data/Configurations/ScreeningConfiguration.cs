using CloudCure.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudCure.Infrastructure.Data.Configurations;

public class ScreeningConfiguration : IEntityTypeConfiguration<Screening>
{
    public void Configure(EntityTypeBuilder<Screening> builder)
    {
        builder.ToTable("screenings");
        builder.HasKey(s => s.Id);

        builder.HasOne(s => s.ScreeningTemplate)
            .WithMany(t => t.Screenings)
            .HasForeignKey(s => s.ScreeningTemplateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Patient)
            .WithMany(p => p.Screenings)
            .HasForeignKey(s => s.PatientId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Encounter)
            .WithMany(e => e.Screenings)
            .HasForeignKey(s => s.EncounterId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(s => s.CompletedByPerson)
            .WithMany()
            .HasForeignKey(s => s.CompletedByPersonId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
