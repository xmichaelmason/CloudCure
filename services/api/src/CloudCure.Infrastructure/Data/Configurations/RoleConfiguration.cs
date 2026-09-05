using CloudCure.Domain.Entities;
using CloudCure.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudCure.Infrastructure.Data.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasConversion<short>().ValueGeneratedNever();
        builder.Property(r => r.Name).IsRequired().HasMaxLength(50);
        builder.HasIndex(r => r.Name).IsUnique();

        builder.HasData(
            new Role { Id = RoleName.Pending, Name = nameof(RoleName.Pending) },
            new Role { Id = RoleName.Nurse, Name = nameof(RoleName.Nurse) },
            new Role { Id = RoleName.Doctor, Name = nameof(RoleName.Doctor) },
            new Role { Id = RoleName.Admin, Name = nameof(RoleName.Admin) });
    }
}
