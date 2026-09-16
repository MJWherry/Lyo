using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Billing.Postgres.Database;

internal sealed class BillingPersonEntityConfiguration : IEntityTypeConfiguration<BillingPersonEntity>
{
    public void Configure(EntityTypeBuilder<BillingPersonEntity> builder)
    {
        builder.ToTable("person", PostgresBillingOptions.Schema);
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.TenantId).HasColumnType("uuid").HasColumnName("tenant_id");
        builder.Property(e => e.DisplayName).HasMaxLength(300).IsRequired().HasColumnName("display_name");
        builder.Property(e => e.GivenName).HasMaxLength(100).HasColumnName("given_name");
        builder.Property(e => e.FamilyName).HasMaxLength(100).HasColumnName("family_name");
        builder.Property(e => e.Title).HasMaxLength(200).HasColumnName("title");
        builder.Property(e => e.CreatedTimestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
        builder.HasIndex(e => e.TenantId).HasDatabaseName("ix_billing_person_tenant").HasFilter("\"tenant_id\" IS NOT NULL");
    }
}

internal sealed class BillingPhoneNumberEntityConfiguration : IEntityTypeConfiguration<BillingPhoneNumberEntity>
{
    public void Configure(EntityTypeBuilder<BillingPhoneNumberEntity> builder)
    {
        builder.ToTable("phone_number", PostgresBillingOptions.Schema);
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.TenantId).HasColumnType("uuid").HasColumnName("tenant_id");
        builder.Property(e => e.Number).HasMaxLength(32).IsRequired().HasColumnName("number");
        builder.Property(e => e.Country).HasMaxLength(2).HasColumnName("country");
        builder.Property(e => e.CreatedTimestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
    }
}

internal sealed class BillingEmailAddressEntityConfiguration : IEntityTypeConfiguration<BillingEmailAddressEntity>
{
    public void Configure(EntityTypeBuilder<BillingEmailAddressEntity> builder)
    {
        builder.ToTable("email_address", PostgresBillingOptions.Schema);
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.TenantId).HasColumnType("uuid").HasColumnName("tenant_id");
        builder.Property(e => e.Email).HasMaxLength(320).IsRequired().HasColumnName("email");
        builder.Property(e => e.CreatedTimestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
    }
}

internal sealed class BillingAddressPayloadEntityConfiguration : IEntityTypeConfiguration<BillingAddressPayloadEntity>
{
    public void Configure(EntityTypeBuilder<BillingAddressPayloadEntity> builder)
    {
        builder.ToTable("address_payload", PostgresBillingOptions.Schema);
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.TenantId).HasColumnType("uuid").HasColumnName("tenant_id");
        builder.Property(e => e.Line1).HasMaxLength(300).HasColumnName("line1");
        builder.Property(e => e.Line2).HasMaxLength(300).HasColumnName("line2");
        builder.Property(e => e.City).HasMaxLength(120).HasColumnName("city");
        builder.Property(e => e.Region).HasMaxLength(120).HasColumnName("region");
        builder.Property(e => e.PostalCode).HasMaxLength(32).HasColumnName("postal_code");
        builder.Property(e => e.Country).HasMaxLength(2).HasColumnName("country");
        builder.Property(e => e.CreatedTimestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
    }
}
