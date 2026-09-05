using CloudCure.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudCure.Infrastructure.Data.Configurations;

/// <summary>
/// Seeded categorized checklist replacing the old app's 825-line hardcoded pixel-coordinate
/// body-clicker diagram. Grouped by region_group so the UI can render a simple accordion/checklist.
/// </summary>
public class BodyRegionConfiguration : IEntityTypeConfiguration<BodyRegion>
{
    public void Configure(EntityTypeBuilder<BodyRegion> builder)
    {
        builder.ToTable("body_regions");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Code).IsRequired().HasMaxLength(50);
        builder.Property(b => b.DisplayName).IsRequired().HasMaxLength(100);
        builder.Property(b => b.RegionGroup).IsRequired().HasMaxLength(50);

        builder.HasIndex(b => b.Code).IsUnique();

        builder.HasData(
            new BodyRegion { Id = 1, Code = "head", DisplayName = "Head", RegionGroup = "Head/Neck" },
            new BodyRegion { Id = 2, Code = "face", DisplayName = "Face", RegionGroup = "Head/Neck" },
            new BodyRegion { Id = 3, Code = "neck", DisplayName = "Neck", RegionGroup = "Head/Neck" },
            new BodyRegion { Id = 4, Code = "chest", DisplayName = "Chest", RegionGroup = "Torso" },
            new BodyRegion { Id = 5, Code = "abdomen", DisplayName = "Abdomen", RegionGroup = "Torso" },
            new BodyRegion { Id = 6, Code = "pelvis", DisplayName = "Pelvis", RegionGroup = "Torso" },
            new BodyRegion { Id = 7, Code = "upper_back", DisplayName = "Upper Back", RegionGroup = "Back" },
            new BodyRegion { Id = 8, Code = "lower_back", DisplayName = "Lower Back", RegionGroup = "Back" },
            new BodyRegion { Id = 9, Code = "shoulder_left", DisplayName = "Left Shoulder", RegionGroup = "Upper Limbs" },
            new BodyRegion { Id = 10, Code = "shoulder_right", DisplayName = "Right Shoulder", RegionGroup = "Upper Limbs" },
            new BodyRegion { Id = 11, Code = "arm_left", DisplayName = "Left Arm", RegionGroup = "Upper Limbs" },
            new BodyRegion { Id = 12, Code = "arm_right", DisplayName = "Right Arm", RegionGroup = "Upper Limbs" },
            new BodyRegion { Id = 13, Code = "hand_left", DisplayName = "Left Hand", RegionGroup = "Upper Limbs" },
            new BodyRegion { Id = 14, Code = "hand_right", DisplayName = "Right Hand", RegionGroup = "Upper Limbs" },
            new BodyRegion { Id = 15, Code = "hip_left", DisplayName = "Left Hip", RegionGroup = "Lower Limbs" },
            new BodyRegion { Id = 16, Code = "hip_right", DisplayName = "Right Hip", RegionGroup = "Lower Limbs" },
            new BodyRegion { Id = 17, Code = "leg_left", DisplayName = "Left Leg", RegionGroup = "Lower Limbs" },
            new BodyRegion { Id = 18, Code = "leg_right", DisplayName = "Right Leg", RegionGroup = "Lower Limbs" },
            new BodyRegion { Id = 19, Code = "foot_left", DisplayName = "Left Foot", RegionGroup = "Lower Limbs" },
            new BodyRegion { Id = 20, Code = "foot_right", DisplayName = "Right Foot", RegionGroup = "Lower Limbs" });
    }
}
