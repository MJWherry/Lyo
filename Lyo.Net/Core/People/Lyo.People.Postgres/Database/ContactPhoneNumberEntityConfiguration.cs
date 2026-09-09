using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.People.Postgres.Database;

public sealed class ContactPhoneNumberEntityConfiguration : IEntityTypeConfiguration<ContactPhoneNumberEntity>
{
    public void Configure(EntityTypeBuilder<ContactPhoneNumberEntity> builder)
    {
        builder.ToTable("contact_phone_number");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.PersonId).HasColumnName("person_id");
        builder.Property(e => e.PhoneNumberId).HasColumnName("phone_number_id");
        builder.Property(e => e.Type).HasMaxLength(20).IsRequired().HasColumnName("type");
        builder.Property(e => e.IsPrimary).HasColumnName("is_primary");
        builder.Property(e => e.StartDate).HasColumnType("date").HasColumnName("start_date");
        builder.Property(e => e.EndDate).HasColumnType("date").HasColumnName("end_date");
        builder.Property(e => e.Notes).HasMaxLength(500).HasColumnName("notes");
        builder.Property(e => e.CreatedTimestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
        builder.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
        builder.HasIndex(e => e.PersonId).HasDatabaseName("ix_contact_phone_number_person_id");
        builder.HasIndex(e => e.PhoneNumberId).HasDatabaseName("ix_contact_phone_number_phone_number_id");
        builder.HasOne(e => e.Person).WithMany(p => p.ContactPhoneNumbers).HasForeignKey(e => e.PersonId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.PhoneNumber).WithMany().HasForeignKey(e => e.PhoneNumberId).OnDelete(DeleteBehavior.Restrict);
    }
}