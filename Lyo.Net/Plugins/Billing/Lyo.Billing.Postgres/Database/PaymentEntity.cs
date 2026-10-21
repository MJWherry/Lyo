using System.ComponentModel.DataAnnotations;

namespace Lyo.Billing.Postgres.Database;

/// <summary>Payment against an invoice.</summary>
public sealed class PaymentEntity
{
    [Key]
    public Guid Id { get; set; }

    public Guid? TenantId { get; set; }

    public Guid InvoiceId { get; set; }

    public decimal Amount { get; set; }

    [MaxLength(3)]
    public string Currency { get; set; } = "USD";

    public int Method { get; set; }

    [MaxLength(120)]
    public string? Reference { get; set; }

    public Guid? JournalId { get; set; }

    public DateTime ReceivedAt { get; set; }

    public InvoiceEntity Invoice { get; set; } = null!;
}