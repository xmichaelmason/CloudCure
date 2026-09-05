using CloudCure.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudCure.Infrastructure.Data.Configurations;

public class ScreeningTemplateConfiguration : IEntityTypeConfiguration<ScreeningTemplate>
{
    public static readonly DateTimeOffset SeedTimestamp = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public void Configure(EntityTypeBuilder<ScreeningTemplate> builder)
    {
        builder.ToTable("screening_templates");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Code).IsRequired().HasMaxLength(50);
        builder.Property(t => t.Title).IsRequired().HasMaxLength(200);

        builder.HasIndex(t => t.Code).IsUnique();

        builder.HasData(new ScreeningTemplate
        {
            Id = 1,
            Code = "covid19_v1",
            Title = "COVID-19 Screening",
            IsActive = true,
            CreatedAt = SeedTimestamp,
        });
    }
}
