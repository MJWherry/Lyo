using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Endato.Postgres.Database;

public sealed class EndatoCePersonEntityConfiguration : IEntityTypeConfiguration<EndatoCePersonEntity>
{
    public void Configure(EntityTypeBuilder<EndatoCePersonEntity> builder)
    {
        builder.ToTable("endato_ce_person");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.FirstName).HasMaxLength(25).IsRequired().HasColumnName("first_name");
        builder.Property(e => e.MiddleName).HasMaxLength(25).HasColumnName("middle_name");
        builder.Property(e => e.LastName).HasMaxLength(25).IsRequired().HasColumnName("last_name");
        builder.Property(e => e.DateOfBirth).HasColumnName("date_of_birth");
        builder.HasMany(e => e.Addresses).WithOne(a => a.EndatoCePerson).HasForeignKey(a => a.EndatoCePersonId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(e => e.PhoneNumbers).WithOne(pn => pn.EndatoCePerson).HasForeignKey(pn => pn.EndatoCePersonId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(e => e.EmailAddresses).WithOne(ea => ea.EndatoCePerson).HasForeignKey(ea => ea.EndatoCePersonId).OnDelete(DeleteBehavior.Cascade);
    }
}