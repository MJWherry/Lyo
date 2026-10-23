using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace Lyo.Ledger.Postgres.Database;

/// <summary>Journal header.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class LedgerJournalEntity
{
    [Key]
    public Guid Id { get; set; }

    public Guid? TenantId { get; set; }

    public int Status { get; set; }

    [MaxLength(4000)]
    public string? Memo { get; set; }

    [MaxLength(200)]
    public string? SourceEntityType { get; set; }

    [MaxLength(200)]
    public string? SourceEntityId { get; set; }

    public Guid? VoidOfJournalId { get; set; }

    public DateTime? PostedAt { get; set; }

    public DateTime? VoidedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public LedgerJournalEntity? VoidOf { get; set; }

    public List<LedgerJournalEntity> Reversals { get; set; } = [];

    public List<LedgerJournalLineEntity> Lines { get; set; } = [];

    /// <summary>Type name and id.</summary>
    public override string ToString() => $"{GetType().Name} {Id}";
}