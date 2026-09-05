using CloudCure.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudCure.Infrastructure.Data.Configurations;

public class AuthIdentityConfiguration : IEntityTypeConfiguration<AuthIdentity>
{
    public void Configure(EntityTypeBuilder<AuthIdentity> builder)
    {
        builder.ToTable("auth_identities");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Auth0Subject).IsRequired().HasMaxLength(255);
        builder.HasIndex(a => a.Auth0Subject).IsUnique();

        builder.HasOne(a => a.Person)
            .WithMany(p => p.AuthIdentities)
            .HasForeignKey(a => a.PersonId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
