using CloudCure.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudCure.Infrastructure.Data.Configurations;

public class PersonRoleConfiguration : IEntityTypeConfiguration<PersonRole>
{
    public void Configure(EntityTypeBuilder<PersonRole> builder)
    {
        builder.ToTable("person_roles");
        builder.HasKey(pr => new { pr.PersonId, pr.RoleId });

        builder.HasOne(pr => pr.Person)
            .WithMany(p => p.PersonRoles)
            .HasForeignKey(pr => pr.PersonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pr => pr.Role)
            .WithMany(r => r.PersonRoles)
            .HasForeignKey(pr => pr.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pr => pr.GrantedByPerson)
            .WithMany()
            .HasForeignKey(pr => pr.GrantedByPersonId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
