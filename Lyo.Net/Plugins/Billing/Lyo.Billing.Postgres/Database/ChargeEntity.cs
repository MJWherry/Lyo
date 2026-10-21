using System.ComponentModel.DataAnnotations;

namespace Lyo.Billing.Postgres.Database;

/// <summary>Unbilled work row.</summary>
public sealed class ChargeEntity
{
    [Key]
    public Guid Id { get; set; }

    public Guid? TenantId { get; set; }

    public Guid ClientId { get; set; }

    public Guid? InvoiceId { get; set; }

    public int Kind { get; set; }

    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public decimal UnitAmount { get; set; }

    [MaxLength(3)]
    public string Currency { get; set; } = "USD";

    public DateTime? OccurredAt { get; set; }

    public DateTime CreatedAt { get; set; }

    [MaxLength(200)]
    public string? CreatedByEntityType { get; set; }

    [MaxLength(200)]
    public string? CreatedByEntityId { get; set; }

    public InvoiceEntity? Invoice { get; set; }

    public BillingClientEntity? Client { get; set; }
}