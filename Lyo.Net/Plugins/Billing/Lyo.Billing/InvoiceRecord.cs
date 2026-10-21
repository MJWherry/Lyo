using Lyo.EntityReference.Models;

namespace Lyo.Billing;

/// <summary>Commercial invoice. Tax ids on this record are plaintext after decrypt; ciphertext is EF-only.</summary>
[EntityRefLogicalType("Invoice")]
public sealed class InvoiceRecord
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }

    /// <summary>Tenant scope.</summary>
    public Guid? TenantId { get; set; }

    /// <summary>Bill-to client.</summary>
    public Guid ClientId { get; set; }

    /// <summary>Assigned at issue (for example <c>INV-2026-00001</c>).</summary>
    public string? Number { get; set; }

    /// <summary>Lifecycle.</summary>
    public InvoiceStatus Status { get; set; }

    /// <summary>ISO 4217.</summary>
    public string Currency { get; set; } = "USD";

    /// <summary>Tax rate applied at issue (0.08 = 8%). Ignored when the client is tax-exempt.</summary>
    public decimal TaxRate { get; set; }

    /// <summary>Sum of line amounts.</summary>
    public decimal Subtotal { get; set; }

    /// <summary>Tax on subtotal.</summary>
    public decimal TaxAmount { get; set; }

    /// <summary>Subtotal + tax.</summary>
    public decimal Total { get; set; }

    /// <summary>Sum of recorded payments.</summary>
    public decimal AmountPaid { get; set; }

    /// <summary>When issued (UTC).</summary>
    public DateTime? IssuedAt { get; set; }

    /// <summary>Due date (UTC date).</summary>
    public DateTime? DueAt { get; set; }

    /// <summary>Issue journal, if posted.</summary>
    public Guid? IssueJournalId { get; set; }

    /// <summary>Snapshot: bill-to name.</summary>
    public string? BillToName { get; set; }

    /// <summary>Snapshot: billing contact display name.</summary>
    public string? BillToContactName { get; set; }

    /// <summary>Snapshot: address line 1.</summary>
    public string? BillToLine1 { get; set; }

    /// <summary>Snapshot: address line 2.</summary>
    public string? BillToLine2 { get; set; }

    /// <summary>Snapshot: city.</summary>
    public string? BillToCity { get; set; }

    /// <summary>Snapshot: region.</summary>
    public string? BillToRegion { get; set; }

    /// <summary>Snapshot: postal code.</summary>
    public string? BillToPostalCode { get; set; }

    /// <summary>Snapshot: country.</summary>
    public string? BillToCountry { get; set; }

    /// <summary>Decrypted tax id snapshot.</summary>
    public string? TaxId { get; set; }

    /// <summary>Last four of the snapshot tax id.</summary>
    public string? TaxIdLast4 { get; set; }

    /// <summary>Notes copied from the client at draft/issue.</summary>
    public string? Notes { get; set; }

    /// <summary>When the row was created (UTC).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Frozen lines.</summary>
    public IReadOnlyList<InvoiceLineRecord> Lines { get; set; } = [];
}