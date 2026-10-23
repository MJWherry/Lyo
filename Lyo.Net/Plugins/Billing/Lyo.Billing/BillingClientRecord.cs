using Lyo.EntityReference.Models;

namespace Lyo.Billing;

/// <summary>Bill-to client owned by billing (schema <c>billing</c>). Tax ids are plaintext on this record after decrypt; ciphertext is EF-only.</summary>
[EntityRefLogicalType("BillingClient")]
public sealed class BillingClientRecord
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }

    /// <summary>Tenant scope. Null means system / no tenant.</summary>
    public Guid? TenantId { get; set; }

    /// <summary>What you call them.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Legal name copied onto issued invoices.</summary>
    public string? LegalName { get; set; }

    /// <summary>Lifecycle status.</summary>
    public BillingClientStatus Status { get; set; } = BillingClientStatus.Active;

    /// <summary>True when this client is tax-exempt. Issued invoices then have a zero tax amount.</summary>
    public bool TaxExempt { get; set; }

    /// <summary>EIN / VAT / GST. Decrypted in memory; never stored in the plaintext column when encryption is on.</summary>
    public string? TaxId { get; set; }

    /// <summary>Last four of the tax id for display and search. Not a secret.</summary>
    public string? TaxIdLast4 { get; set; }

    /// <summary>Default ISO 4217 currency (for example <c>USD</c>).</summary>
    public string DefaultCurrency { get; set; } = "USD";

    /// <summary>Net days used when issuing invoices (Net 30 → 30).</summary>
    public int PaymentTermsDays { get; set; } = 30;

    /// <summary>Prefix for issued invoice numbers (for example <c>ACME</c>).</summary>
    public string? InvoicePrefix { get; set; }

    /// <summary>Default notes copied onto new invoices.</summary>
    public string? DefaultInvoiceNotes { get; set; }

    /// <summary>Bill-to contact name snapshotted at issue.</summary>
    public string? ContactName { get; set; }

    /// <summary>Bill-to address line 1.</summary>
    public string? Line1 { get; set; }

    /// <summary>Bill-to address line 2.</summary>
    public string? Line2 { get; set; }

    /// <summary>Bill-to city.</summary>
    public string? City { get; set; }

    /// <summary>Bill-to region / state.</summary>
    public string? Region { get; set; }

    /// <summary>Bill-to postal code.</summary>
    public string? PostalCode { get; set; }

    /// <summary>ISO 3166-1 alpha-2 country.</summary>
    public string? Country { get; set; }

    /// <summary>When the row was created (UTC).</summary>
    public DateTime CreatedAt { get; set; }
}