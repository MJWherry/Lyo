using System.ComponentModel.DataAnnotations;

namespace Lyo.Billing.Postgres.Database;

/// <summary>Per-tenant per-year invoice number sequence.</summary>
public sealed class InvoiceSequenceEntity
{
    [Key]
    public Guid Id { get; set; }

    public Guid? TenantId { get; set; }

    public int Year { get; set; }

    public int LastValue { get; set; }
}
