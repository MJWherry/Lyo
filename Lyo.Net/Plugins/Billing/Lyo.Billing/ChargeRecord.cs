using Lyo.EntityReference.Models;

namespace Lyo.Billing;

/// <summary>Unbilled time or expense. <see cref="InvoiceId" /> is set when the invoice is issued.</summary>
public sealed class ChargeRecord
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }

    /// <summary>Tenant scope.</summary>
    public Guid? TenantId { get; set; }

    /// <summary>Client this charge belongs to.</summary>
    public Guid ClientId { get; set; }

    /// <summary>Issued invoice, if any.</summary>
    public Guid? InvoiceId { get; set; }

    /// <summary>Time, expense, product, or other.</summary>
    public ChargeKind Kind { get; set; }

    /// <summary>Line description copied onto the invoice.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Quantity (hours, units).</summary>
    public decimal Quantity { get; set; } = 1m;

    /// <summary>Unit amount in <see cref="Currency" />.</summary>
    public decimal UnitAmount { get; set; }

    /// <summary>ISO 4217 currency.</summary>
    public string Currency { get; set; } = "USD";

    /// <summary>When the work occurred (UTC date).</summary>
    public DateTime? OccurredAt { get; set; }

    /// <summary>When the row was created (UTC).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Optional actor.</summary>
    public EntityRef? CreatedBy { get; set; }
}