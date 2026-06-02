using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.People.Postgres.Database;

public sealed class ContactAddressEntityConfiguration : IEntityTypeConfiguration<ContactAddressEntity>
{
    public void Configure(EntityTypeBuilder<ContactAddressEntity> builder)
    {
        builder.ToTable("contact_address");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.PersonId).HasColumnName("person_id");
        builder.Property(e => e.AddressId).HasColumnName("address_id");
        builder.Property(e => e.Type).HasMaxLength(20).IsRequired().HasColumnName("type");
        builder.Property(e => e.IsPrimary).HasColumnName("is_primary");
        builder.Property(e => e.StartDate).HasColumnType("date").HasColumnName("start_date");
        builder.Property(e => e.EndDate).HasColumnType("date").HasColumnName("end_date");
        builder.Property(e => e.Notes).HasMaxLength(500).HasColumnName("notes");
        builder.Property(e => e.CreatedTimestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
        builder.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
        builder.HasIndex(e => e.PersonId).HasDatabaseName("ix_contact_address_person_id");
        builder.HasIndex(e => e.AddressId).HasDatabaseName("ix_contact_address_address_id");
        builder.HasOne(e => e.Person).WithMany(p => p.ContactAddresses).HasForeignKey(e => e.PersonId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Address).WithMany().HasForeignKey(e => e.AddressId).OnDelete(DeleteBehavior.Restrict);
    }
}