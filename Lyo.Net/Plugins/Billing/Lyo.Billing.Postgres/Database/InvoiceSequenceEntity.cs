using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace Lyo.Billing.Postgres.Database;

/// <summary>Per-tenant per-year invoice number sequence.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class InvoiceSequenceEntity
{
    [Key]
    public Guid Id { get; set; }

    public Guid? TenantId { get; set; }

    public int Year { get; set; }

    public int LastValue { get; set; }

    /// <summary>Type name and id.</summary>
    public override string ToString() => $"{GetType().Name} {Id}";
}