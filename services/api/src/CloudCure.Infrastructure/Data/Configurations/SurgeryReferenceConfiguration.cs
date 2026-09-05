using CloudCure.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudCure.Infrastructure.Data.Configurations;

public class SurgeryReferenceConfiguration : IEntityTypeConfiguration<SurgeryReference>
{
    public void Configure(EntityTypeBuilder<SurgeryReference> builder)
    {
        builder.ToTable("surgery_reference");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.CanonicalName).IsRequired().HasMaxLength(200);
        builder.HasIndex(r => r.CanonicalName).IsUnique();
    }
}
