using CloudCure.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudCure.Infrastructure.Data.Configurations;

public class VitalsConfiguration : IEntityTypeConfiguration<Vitals>
{
    public void Configure(EntityTypeBuilder<Vitals> builder)
    {
        builder.ToTable("vitals");
        builder.HasKey(v => v.Id);

        builder.Property(v => v.OxygenSaturationPct).HasPrecision(4, 1);
        builder.Property(v => v.TemperatureValue).HasPrecision(4, 1);
        builder.Property(v => v.HeightValue).HasPrecision(5, 1);
        builder.Property(v => v.WeightValue).HasPrecision(5, 1);

        builder.HasOne(v => v.Encounter)
            .WithOne(e => e.Vitals)
            .HasForeignKey<Vitals>(v => v.EncounterId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
