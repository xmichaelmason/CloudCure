using CloudCure.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudCure.Infrastructure.Data.Configurations;

public class EncounterConfiguration : IEntityTypeConfiguration<Encounter>
{
    public void Configure(EntityTypeBuilder<Encounter> builder)
    {
        builder.ToTable("encounters");
        builder.HasKey(e => e.Id);

        builder.HasOne(e => e.Patient)
            .WithMany(p => p.Encounters)
            .HasForeignKey(e => e.PatientId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.AttendingStaffMember)
            .WithMany(s => s.AttendedEncounters)
            .HasForeignKey(e => e.AttendingStaffMemberId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
