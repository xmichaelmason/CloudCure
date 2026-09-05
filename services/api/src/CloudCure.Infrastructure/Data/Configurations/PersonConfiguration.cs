using CloudCure.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudCure.Infrastructure.Data.Configurations;

public class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        builder.ToTable("people");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(p => p.LastName).IsRequired().HasMaxLength(100);
        builder.Property(p => p.PhoneE164).HasMaxLength(20);
        builder.Property(p => p.Email).HasMaxLength(320);

        builder.HasIndex(p => p.Email).IsUnique().HasFilter("email IS NOT NULL");

        builder.HasOne(p => p.Address)
            .WithOne(a => a.Person)
            .HasForeignKey<Address>(a => a.PersonId);

        builder.HasOne(p => p.Patient)
            .WithOne(pt => pt.Person)
            .HasForeignKey<Patient>(pt => pt.PersonId);

        builder.HasOne(p => p.StaffMember)
            .WithOne(s => s.Person)
            .HasForeignKey<StaffMember>(s => s.PersonId);
    }
}
