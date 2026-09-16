using System.ComponentModel.DataAnnotations;

namespace Lyo.Billing.Postgres.Database;

/// <summary>Frozen invoice line.</summary>
public sealed class InvoiceLineEntity
{
    [Key]
    public Guid Id { get; set; }

    public Guid InvoiceId { get; set; }

    public Guid? ChargeId { get; set; }

    public int LineNumber { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public decimal UnitAmount { get; set; }

    public decimal Amount { get; set; }

    public int Pricing { get; set; }

    public InvoiceEntity Invoice { get; set; } = null!;
}
