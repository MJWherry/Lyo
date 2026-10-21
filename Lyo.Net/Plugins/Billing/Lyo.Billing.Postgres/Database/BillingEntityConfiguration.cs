using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Billing.Postgres.Database;

internal sealed class BillingClientEntityConfiguration : IEntityTypeConfiguration<BillingClientEntity>
{
    public void Configure(EntityTypeBuilder<BillingClientEntity> builder)
    {
        builder.ToTable("client", PostgresBillingOptions.Schema);
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.TenantId).HasColumnType("uuid").HasColumnName("tenant_id");
        builder.Property(e => e.DisplayName).HasMaxLength(300).IsRequired().HasColumnName("display_name");
        builder.Property(e => e.LegalName).HasMaxLength(300).HasColumnName("legal_name");
        builder.Property(e => e.Status).HasColumnName("status");
        builder.Property(e => e.TaxExempt).HasColumnName("tax_exempt");
        builder.Property(e => e.TaxId).HasMaxLength(64).HasColumnName("tax_id");
        builder.Property(e => e.EncryptedTaxId).HasColumnName("encrypted_tax_id").HasColumnType("bytea");
        builder.Property(e => e.TaxIdLast4).HasMaxLength(8).HasColumnName("tax_id_last4");
        builder.Property(e => e.DefaultCurrency).HasMaxLength(3).IsRequired().HasColumnName("default_currency");
        builder.Property(e => e.PaymentTermsDays).HasColumnName("payment_terms_days");
        builder.Property(e => e.InvoicePrefix).HasMaxLength(32).HasColumnName("invoice_prefix");
        builder.Property(e => e.DefaultInvoiceNotes).HasMaxLength(4000).HasColumnName("default_invoice_notes");
        builder.Property(e => e.ContactName).HasMaxLength(300).HasColumnName("contact_name");
        builder.Property(e => e.Line1).HasMaxLength(300).HasColumnName("line1");
        builder.Property(e => e.Line2).HasMaxLength(300).HasColumnName("line2");
        builder.Property(e => e.City).HasMaxLength(120).HasColumnName("city");
        builder.Property(e => e.Region).HasMaxLength(120).HasColumnName("region");
        builder.Property(e => e.PostalCode).HasMaxLength(32).HasColumnName("postal_code");
        builder.Property(e => e.Country).HasMaxLength(2).HasColumnName("country");
        builder.Property(e => e.CreatedAt).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_at");
        builder.HasIndex(e => e.TenantId).HasDatabaseName("ix_billing_client_tenant").HasFilter("\"tenant_id\" IS NOT NULL");
        builder.HasIndex(e => e.TaxIdLast4).HasDatabaseName("ix_billing_client_tax_id_last4").HasFilter("\"tax_id_last4\" IS NOT NULL");
    }
}

internal sealed class ChargeEntityConfiguration : IEntityTypeConfiguration<ChargeEntity>
{
    public void Configure(EntityTypeBuilder<ChargeEntity> builder)
    {
        builder.ToTable("charge", PostgresBillingOptions.Schema);
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.TenantId).HasColumnType("uuid").HasColumnName("tenant_id");
        builder.Property(e => e.ClientId).IsRequired().HasColumnName("client_id").HasColumnType("uuid");
        builder.Property(e => e.InvoiceId).HasColumnName("invoice_id").HasColumnType("uuid");
        builder.Property(e => e.Kind).HasColumnName("kind");
        builder.Property(e => e.Description).HasMaxLength(500).IsRequired().HasColumnName("description");
        builder.Property(e => e.Quantity).HasColumnType("numeric(18,4)").HasColumnName("quantity");
        builder.Property(e => e.UnitAmount).HasColumnType("numeric(18,4)").HasColumnName("unit_amount");
        builder.Property(e => e.Currency).HasMaxLength(3).IsRequired().HasColumnName("currency");
        builder.Property(e => e.OccurredAt).HasColumnType("timestamp with time zone").HasColumnName("occurred_at");
        builder.Property(e => e.CreatedAt).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_at");
        builder.Property(e => e.CreatedByEntityType).HasMaxLength(200).HasColumnName("created_by_entity_type");
        builder.Property(e => e.CreatedByEntityId).HasMaxLength(200).HasColumnName("created_by_entity_id");
        builder.HasOne(e => e.Client).WithMany(e => e.Charges).HasForeignKey(e => e.ClientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.Invoice).WithMany(e => e.Charges).HasForeignKey(e => e.InvoiceId).OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(e => e.ClientId).HasDatabaseName("ix_billing_charge_client");
        builder.HasIndex(e => e.InvoiceId).HasDatabaseName("ix_billing_charge_invoice");
        builder.HasIndex(e => e.TenantId).HasDatabaseName("ix_billing_charge_tenant").HasFilter("\"tenant_id\" IS NOT NULL");
    }
}

internal sealed class InvoiceEntityConfiguration : IEntityTypeConfiguration<InvoiceEntity>
{
    public void Configure(EntityTypeBuilder<InvoiceEntity> builder)
    {
        builder.ToTable("invoice", PostgresBillingOptions.Schema);
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.TenantId).HasColumnType("uuid").HasColumnName("tenant_id");
        builder.Property(e => e.ClientId).IsRequired().HasColumnName("client_id").HasColumnType("uuid");
        builder.Property(e => e.Number).HasMaxLength(64).HasColumnName("number");
        builder.Property(e => e.Status).HasColumnName("status");
        builder.Property(e => e.Currency).HasMaxLength(3).IsRequired().HasColumnName("currency");
        builder.Property(e => e.TaxRate).HasColumnType("numeric(18,4)").HasColumnName("tax_rate");
        builder.Property(e => e.Subtotal).HasColumnType("numeric(18,4)").HasColumnName("subtotal");
        builder.Property(e => e.TaxAmount).HasColumnType("numeric(18,4)").HasColumnName("tax_amount");
        builder.Property(e => e.Total).HasColumnType("numeric(18,4)").HasColumnName("total");
        builder.Property(e => e.AmountPaid).HasColumnType("numeric(18,4)").HasColumnName("amount_paid");
        builder.Property(e => e.IssuedAt).HasColumnType("timestamp with time zone").HasColumnName("issued_at");
        builder.Property(e => e.DueAt).HasColumnType("timestamp with time zone").HasColumnName("due_at");
        builder.Property(e => e.IssueJournalId).HasColumnName("issue_journal_id").HasColumnType("uuid");
        builder.Property(e => e.BillToName).HasMaxLength(300).HasColumnName("bill_to_name");
        builder.Property(e => e.BillToContactName).HasMaxLength(300).HasColumnName("bill_to_contact_name");
        builder.Property(e => e.BillToLine1).HasMaxLength(300).HasColumnName("bill_to_line1");
        builder.Property(e => e.BillToLine2).HasMaxLength(300).HasColumnName("bill_to_line2");
        builder.Property(e => e.BillToCity).HasMaxLength(120).HasColumnName("bill_to_city");
        builder.Property(e => e.BillToRegion).HasMaxLength(120).HasColumnName("bill_to_region");
        builder.Property(e => e.BillToPostalCode).HasMaxLength(32).HasColumnName("bill_to_postal_code");
        builder.Property(e => e.BillToCountry).HasMaxLength(2).HasColumnName("bill_to_country");
        builder.Property(e => e.TaxId).HasMaxLength(64).HasColumnName("tax_id");
        builder.Property(e => e.EncryptedTaxId).HasColumnName("encrypted_tax_id").HasColumnType("bytea");
        builder.Property(e => e.TaxIdLast4).HasMaxLength(8).HasColumnName("tax_id_last4");
        builder.Property(e => e.Notes).HasMaxLength(4000).HasColumnName("notes");
        builder.Property(e => e.CreatedAt).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_at");
        builder.HasOne(e => e.Client).WithMany(e => e.Invoices).HasForeignKey(e => e.ClientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(e => new { e.TenantId, e.Number }).IsUnique().HasDatabaseName("uq_billing_invoice_tenant_number").HasFilter("\"number\" IS NOT NULL");
        builder.HasIndex(e => e.ClientId).HasDatabaseName("ix_billing_invoice_client");
        builder.HasIndex(e => e.Status).HasDatabaseName("ix_billing_invoice_status");
    }
}

internal sealed class InvoiceLineEntityConfiguration : IEntityTypeConfiguration<InvoiceLineEntity>
{
    public void Configure(EntityTypeBuilder<InvoiceLineEntity> builder)
    {
        builder.ToTable("invoice_line", PostgresBillingOptions.Schema);
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.InvoiceId).IsRequired().HasColumnName("invoice_id").HasColumnType("uuid");
        builder.Property(e => e.ChargeId).HasColumnName("charge_id").HasColumnType("uuid");
        builder.Property(e => e.LineNumber).HasColumnName("line_number");
        builder.Property(e => e.Title).HasMaxLength(200).IsRequired().HasColumnName("title");
        builder.Property(e => e.Description).HasMaxLength(500).IsRequired().HasColumnName("description");
        builder.Property(e => e.Quantity).HasColumnType("numeric(18,4)").HasColumnName("quantity");
        builder.Property(e => e.UnitAmount).HasColumnType("numeric(18,4)").HasColumnName("unit_amount");
        builder.Property(e => e.Amount).HasColumnType("numeric(18,4)").HasColumnName("amount");
        builder.Property(e => e.Pricing).HasColumnName("pricing").HasDefaultValue(0);
        builder.HasOne(e => e.Invoice).WithMany(e => e.Lines).HasForeignKey(e => e.InvoiceId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => e.InvoiceId).HasDatabaseName("ix_billing_invoice_line_invoice");
    }
}

internal sealed class PaymentEntityConfiguration : IEntityTypeConfiguration<PaymentEntity>
{
    public void Configure(EntityTypeBuilder<PaymentEntity> builder)
    {
        builder.ToTable("payment", PostgresBillingOptions.Schema);
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.TenantId).HasColumnType("uuid").HasColumnName("tenant_id");
        builder.Property(e => e.InvoiceId).IsRequired().HasColumnName("invoice_id").HasColumnType("uuid");
        builder.Property(e => e.Amount).HasColumnType("numeric(18,4)").HasColumnName("amount");
        builder.Property(e => e.Currency).HasMaxLength(3).IsRequired().HasColumnName("currency");
        builder.Property(e => e.Method).HasColumnName("method");
        builder.Property(e => e.Reference).HasMaxLength(120).HasColumnName("reference");
        builder.Property(e => e.JournalId).HasColumnName("journal_id").HasColumnType("uuid");
        builder.Property(e => e.ReceivedAt).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("received_at");
        builder.HasOne(e => e.Invoice).WithMany(e => e.Payments).HasForeignKey(e => e.InvoiceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(e => e.InvoiceId).HasDatabaseName("ix_billing_payment_invoice");
    }
}

internal sealed class BillerProfileEntityConfiguration : IEntityTypeConfiguration<BillerProfileEntity>
{
    public void Configure(EntityTypeBuilder<BillerProfileEntity> builder)
    {
        builder.ToTable("biller_profile", PostgresBillingOptions.Schema);
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.TenantId).HasColumnType("uuid").HasColumnName("tenant_id");
        builder.Property(e => e.DisplayName).HasMaxLength(300).IsRequired().HasColumnName("display_name");
        builder.Property(e => e.LegalName).HasMaxLength(300).HasColumnName("legal_name");
        builder.Property(e => e.Remittance).HasMaxLength(4000).HasColumnName("remittance");
        builder.Property(e => e.EncryptedRemittance).HasColumnName("encrypted_remittance").HasColumnType("bytea");
        builder.Property(e => e.BankAccountNumber).HasMaxLength(64).HasColumnName("bank_account_number");
        builder.Property(e => e.EncryptedBankAccountNumber).HasColumnName("encrypted_bank_account_number").HasColumnType("bytea");
        builder.Property(e => e.BankAccountLast4).HasMaxLength(8).HasColumnName("bank_account_last4");
        builder.HasIndex(e => e.TenantId).IsUnique().HasDatabaseName("uq_billing_biller_profile_tenant");
    }
}

internal sealed class InvoiceSequenceEntityConfiguration : IEntityTypeConfiguration<InvoiceSequenceEntity>
{
    public void Configure(EntityTypeBuilder<InvoiceSequenceEntity> builder)
    {
        builder.ToTable("invoice_sequence", PostgresBillingOptions.Schema);
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.TenantId).HasColumnType("uuid").HasColumnName("tenant_id");
        builder.Property(e => e.Year).HasColumnName("year");
        builder.Property(e => e.LastValue).HasColumnName("last_value");
        builder.HasIndex(e => new { e.TenantId, e.Year }).IsUnique().HasDatabaseName("uq_billing_invoice_sequence_tenant_year");
    }
}