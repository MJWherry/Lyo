using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Endato.Postgres.Database;

public sealed class EndatoPsPersonEntityConfiguration : IEntityTypeConfiguration<EndatoPsPersonEntity>
{
    public void Configure(EntityTypeBuilder<EndatoPsPersonEntity> builder)
    {
        builder.ToTable("endato_ps_person");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.QueryId).IsRequired().HasColumnName("query_id");
        builder.Property(e => e.Prefix).HasMaxLength(12).HasColumnName("prefix");
        builder.Property(e => e.FirstName).HasMaxLength(25).HasColumnName("first_name");
        builder.Property(e => e.MiddleName).HasMaxLength(25).HasColumnName("middle_name");
        builder.Property(e => e.LastName).HasMaxLength(25).HasColumnName("last_name");
        builder.Property(e => e.Suffix).HasMaxLength(12).HasColumnName("suffix");
        builder.Property(e => e.DateOfBirth).HasColumnName("date_of_birth");
        builder.HasOne(e => e.Query).WithMany(q => q.People).HasForeignKey(e => e.QueryId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(e => e.Addresses).WithOne(a => a.EndatoPerson).HasForeignKey(a => a.EndatoPersonId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(e => e.EmailAddresses).WithOne(ea => ea.EndatoPerson).HasForeignKey(ea => ea.EndatoPersonId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(e => e.PhoneNumbers).WithOne(pn => pn.EndatoPerson).HasForeignKey(pn => pn.EndatoPersonId).OnDelete(DeleteBehavior.Cascade);
    }
}