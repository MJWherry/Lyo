using Lyo.EntityReference.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.People.Postgres.Database;

public sealed class PersonSourceEntityConfiguration : IEntityTypeConfiguration<PersonSourceEntity>
{
    public void Configure(EntityTypeBuilder<PersonSourceEntity> builder)
    {
        builder.ToTable("person_source");
        builder.HasKey(e => e.Id);
        builder.ConfigureEntitySourceColumns("ix_person_source_source_entity");
        builder.Property(e => e.PersonId).HasColumnName("person_id").HasColumnType("uuid");
        builder.HasOne(e => e.Person).WithMany(p => p.Sources).HasForeignKey(e => e.PersonId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class AddressSourceEntityConfiguration : IEntityTypeConfiguration<AddressSourceEntity>
{
    public void Configure(EntityTypeBuilder<AddressSourceEntity> builder)
    {
        builder.ToTable("address_source");
        builder.HasKey(e => e.Id);
        builder.ConfigureEntitySourceColumns("ix_address_source_source_entity");
        builder.Property(e => e.AddressId).HasColumnName("address_id").HasColumnType("uuid");
        builder.HasOne(e => e.Address).WithMany(a => a.Sources).HasForeignKey(e => e.AddressId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class PhoneNumberSourceEntityConfiguration : IEntityTypeConfiguration<PhoneNumberSourceEntity>
{
    public void Configure(EntityTypeBuilder<PhoneNumberSourceEntity> builder)
    {
        builder.ToTable("phone_number_source");
        builder.HasKey(e => e.Id);
        builder.ConfigureEntitySourceColumns("ix_phone_number_source_source_entity");
        builder.Property(e => e.PhoneNumberId).HasColumnName("phone_number_id").HasColumnType("uuid");
        builder.HasOne(e => e.PhoneNumber).WithMany(p => p.Sources).HasForeignKey(e => e.PhoneNumberId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class EmailAddressSourceEntityConfiguration : IEntityTypeConfiguration<EmailAddressSourceEntity>
{
    public void Configure(EntityTypeBuilder<EmailAddressSourceEntity> builder)
    {
        builder.ToTable("email_address_source");
        builder.HasKey(e => e.Id);
        builder.ConfigureEntitySourceColumns("ix_email_address_source_source_entity");
        builder.Property(e => e.EmailAddressId).HasColumnName("email_address_id").HasColumnType("uuid");
        builder.HasOne(e => e.EmailAddress).WithMany(e => e.Sources).HasForeignKey(e => e.EmailAddressId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ContactAddressSourceEntityConfiguration : IEntityTypeConfiguration<ContactAddressSourceEntity>
{
    public void Configure(EntityTypeBuilder<ContactAddressSourceEntity> builder)
    {
        builder.ToTable("contact_address_source");
        builder.HasKey(e => e.Id);
        builder.ConfigureEntitySourceColumns("ix_contact_address_source_source_entity");
        builder.Property(e => e.ContactAddressId).HasColumnName("contact_address_id").HasColumnType("uuid");
        builder.HasOne(e => e.ContactAddress).WithMany(c => c.Sources).HasForeignKey(e => e.ContactAddressId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ContactPhoneNumberSourceEntityConfiguration : IEntityTypeConfiguration<ContactPhoneNumberSourceEntity>
{
    public void Configure(EntityTypeBuilder<ContactPhoneNumberSourceEntity> builder)
    {
        builder.ToTable("contact_phone_number_source");
        builder.HasKey(e => e.Id);
        builder.ConfigureEntitySourceColumns("ix_contact_phone_number_source_source_entity");
        builder.Property(e => e.ContactPhoneNumberId).HasColumnName("contact_phone_number_id").HasColumnType("uuid");
        builder.HasOne(e => e.ContactPhoneNumber).WithMany(c => c.Sources).HasForeignKey(e => e.ContactPhoneNumberId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ContactEmailAddressSourceEntityConfiguration : IEntityTypeConfiguration<ContactEmailAddressSourceEntity>
{
    public void Configure(EntityTypeBuilder<ContactEmailAddressSourceEntity> builder)
    {
        builder.ToTable("contact_email_address_source");
        builder.HasKey(e => e.Id);
        builder.ConfigureEntitySourceColumns("ix_contact_email_address_source_source_entity");
        builder.Property(e => e.ContactEmailAddressId).HasColumnName("contact_email_address_id").HasColumnType("uuid");
        builder.HasOne(e => e.ContactEmailAddress).WithMany(c => c.Sources).HasForeignKey(e => e.ContactEmailAddressId).OnDelete(DeleteBehavior.Cascade);
    }
}
