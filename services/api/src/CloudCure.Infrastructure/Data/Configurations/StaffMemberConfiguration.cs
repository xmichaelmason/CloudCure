using CloudCure.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudCure.Infrastructure.Data.Configurations;

public class StaffMemberConfiguration : IEntityTypeConfiguration<StaffMember>
{
    public void Configure(EntityTypeBuilder<StaffMember> builder)
    {
        builder.ToTable("staff_members");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.WorkEmail).IsRequired().HasMaxLength(320);
        builder.Property(s => s.Specialization).IsRequired().HasMaxLength(200);
        builder.Property(s => s.RoomNumber).HasMaxLength(20);
        builder.Property(s => s.EducationDegree).IsRequired().HasMaxLength(200);

        builder.HasIndex(s => s.WorkEmail).IsUnique();
        builder.HasIndex(s => s.PersonId).IsUnique();
    }
}
