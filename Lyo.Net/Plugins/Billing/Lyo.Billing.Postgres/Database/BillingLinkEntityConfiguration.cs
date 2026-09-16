using Lyo.EntityReference.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Billing.Postgres.Database;

internal sealed class BillingContactEntityConfiguration : EntityRelationConfiguration<BillingContactEntity>
{
    public BillingContactEntityConfiguration()
        : base("billing_contact") { }

    public override void Configure(EntityTypeBuilder<BillingContactEntity> builder)
    {
        builder.ToTable("contact", PostgresBillingOptions.Schema);
        builder.HasKey(e => e.Id);
        base.Configure(builder);
        builder.Property(e => e.ClientId).IsRequired().HasColumnName("client_id").HasColumnType("uuid");
        builder.Property(e => e.Role).HasColumnName("role");
        builder.Property(e => e.Title).HasMaxLength(200).HasColumnName("title");
        builder.Property(e => e.IsPrimary).HasColumnName("is_primary");
        builder.Property(e => e.IsBillingContact).HasColumnName("is_billing_contact");
        builder.HasOne(e => e.Client).WithMany().HasForeignKey(e => e.ClientId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => e.ClientId).HasDatabaseName("ix_billing_contact_client");
    }
}

internal sealed class BillingPhoneEntityConfiguration : EntityRelationConfiguration<BillingPhoneEntity>
{
    public BillingPhoneEntityConfiguration()
        : base("billing_phone") { }

    public override void Configure(EntityTypeBuilder<BillingPhoneEntity> builder)
    {
        builder.ToTable("phone", PostgresBillingOptions.Schema);
        builder.HasKey(e => e.Id);
        base.Configure(builder);
        builder.Property(e => e.ClientId).IsRequired().HasColumnName("client_id").HasColumnType("uuid");
        builder.Property(e => e.Usage).HasColumnName("usage");
        builder.Property(e => e.IsPrimary).HasColumnName("is_primary");
        builder.Property(e => e.ContactEntityType).HasMaxLength(200).HasColumnName("contact_entity_type");
        builder.Property(e => e.ContactEntityId).HasMaxLength(200).HasColumnName("contact_entity_id");
        builder.HasOne(e => e.Client).WithMany().HasForeignKey(e => e.ClientId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => e.ClientId).HasDatabaseName("ix_billing_phone_client");
    }
}

internal sealed class BillingEmailEntityConfiguration : EntityRelationConfiguration<BillingEmailEntity>
{
    public BillingEmailEntityConfiguration()
        : base("billing_email") { }

    public override void Configure(EntityTypeBuilder<BillingEmailEntity> builder)
    {
        builder.ToTable("email", PostgresBillingOptions.Schema);
        builder.HasKey(e => e.Id);
        base.Configure(builder);
        builder.Property(e => e.ClientId).IsRequired().HasColumnName("client_id").HasColumnType("uuid");
        builder.Property(e => e.Usage).HasColumnName("usage");
        builder.Property(e => e.IsPrimary).HasColumnName("is_primary");
        builder.Property(e => e.ContactEntityType).HasMaxLength(200).HasColumnName("contact_entity_type");
        builder.Property(e => e.ContactEntityId).HasMaxLength(200).HasColumnName("contact_entity_id");
        builder.HasOne(e => e.Client).WithMany().HasForeignKey(e => e.ClientId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => e.ClientId).HasDatabaseName("ix_billing_email_client");
    }
}

internal sealed class BillingAddressEntityConfiguration : EntityRelationConfiguration<BillingAddressEntity>
{
    public BillingAddressEntityConfiguration()
        : base("billing_address") { }

    public override void Configure(EntityTypeBuilder<BillingAddressEntity> builder)
    {
        builder.ToTable("address", PostgresBillingOptions.Schema);
        builder.HasKey(e => e.Id);
        base.Configure(builder);
        builder.Property(e => e.ClientId).IsRequired().HasColumnName("client_id").HasColumnType("uuid");
        builder.Property(e => e.Usage).HasColumnName("usage");
        builder.Property(e => e.IsPrimary).HasColumnName("is_primary");
        builder.Property(e => e.ContactEntityType).HasMaxLength(200).HasColumnName("contact_entity_type");
        builder.Property(e => e.ContactEntityId).HasMaxLength(200).HasColumnName("contact_entity_id");
        builder.HasOne(e => e.Client).WithMany().HasForeignKey(e => e.ClientId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => e.ClientId).HasDatabaseName("ix_billing_address_client");
    }
}
