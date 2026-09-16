using System.ComponentModel.DataAnnotations;

namespace Lyo.Billing.Postgres.Database;

/// <summary>Bill-to client in schema <c>billing</c> with encrypted tax id.</summary>
public sealed class BillingClientEntity
{
    [Key]
    public Guid Id { get; set; }

    public Guid? TenantId { get; set; }

    [Required]
    [MaxLength(300)]
    public string DisplayName { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? LegalName { get; set; }

    public int Status { get; set; } = (int)BillingClientStatus.Active;

    public bool TaxExempt { get; set; }

    [MaxLength(64)]
    public string? TaxId { get; set; }

    public byte[]? EncryptedTaxId { get; set; }

    [MaxLength(8)]
    public string? TaxIdLast4 { get; set; }

    [MaxLength(3)]
    public string DefaultCurrency { get; set; } = "USD";

    public int PaymentTermsDays { get; set; } = 30;

    [MaxLength(32)]
    public string? InvoicePrefix { get; set; }

    [MaxLength(4000)]
    public string? DefaultInvoiceNotes { get; set; }

    [MaxLength(300)]
    public string? ContactName { get; set; }

    [MaxLength(300)]
    public string? Line1 { get; set; }

    [MaxLength(300)]
    public string? Line2 { get; set; }

    [MaxLength(120)]
    public string? City { get; set; }

    [MaxLength(120)]
    public string? Region { get; set; }

    [MaxLength(32)]
    public string? PostalCode { get; set; }

    [MaxLength(2)]
    public string? Country { get; set; }

    public DateTime CreatedAt { get; set; }

    public List<InvoiceEntity> Invoices { get; set; } = [];

    public List<ChargeEntity> Charges { get; set; } = [];
}
