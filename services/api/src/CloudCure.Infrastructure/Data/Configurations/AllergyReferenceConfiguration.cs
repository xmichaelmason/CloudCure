using CloudCure.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudCure.Infrastructure.Data.Configurations;

public class AllergyReferenceConfiguration : IEntityTypeConfiguration<AllergyReference>
{
    public void Configure(EntityTypeBuilder<AllergyReference> builder)
    {
        builder.ToTable("allergy_reference");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.CanonicalName).IsRequired().HasMaxLength(200);
        builder.HasIndex(r => r.CanonicalName).IsUnique();
    }
}
