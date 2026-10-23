using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace Lyo.Billing.Postgres.Database;

/// <summary>Invoice header with encrypted tax snapshot.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class InvoiceEntity
{
    [Key]
    public Guid Id { get; set; }

    public Guid? TenantId { get; set; }

    public Guid ClientId { get; set; }

    [MaxLength(64)]
    public string? Number { get; set; }

    public int Status { get; set; }

    [MaxLength(3)]
    public string Currency { get; set; } = "USD";

    public decimal TaxRate { get; set; }

    public decimal Subtotal { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal Total { get; set; }

    public decimal AmountPaid { get; set; }

    public DateTime? IssuedAt { get; set; }

    public DateTime? DueAt { get; set; }

    public Guid? IssueJournalId { get; set; }

    [MaxLength(300)]
    public string? BillToName { get; set; }

    [MaxLength(300)]
    public string? BillToContactName { get; set; }

    [MaxLength(300)]
    public string? BillToLine1 { get; set; }

    [MaxLength(300)]
    public string? BillToLine2 { get; set; }

    [MaxLength(120)]
    public string? BillToCity { get; set; }

    [MaxLength(120)]
    public string? BillToRegion { get; set; }

    [MaxLength(32)]
    public string? BillToPostalCode { get; set; }

    [MaxLength(2)]
    public string? BillToCountry { get; set; }

    [MaxLength(64)]
    public string? TaxId { get; set; }

    public byte[]? EncryptedTaxId { get; set; }

    [MaxLength(8)]
    public string? TaxIdLast4 { get; set; }

    [MaxLength(4000)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public List<InvoiceLineEntity> Lines { get; set; } = [];

    public List<ChargeEntity> Charges { get; set; } = [];

    public List<PaymentEntity> Payments { get; set; } = [];

    public BillingClientEntity? Client { get; set; }

    /// <summary>Type name and id.</summary>
    public override string ToString() => $"{GetType().Name} {Id}";
}