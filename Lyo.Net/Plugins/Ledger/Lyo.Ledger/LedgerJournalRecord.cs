using Lyo.EntityReference.Models;

namespace Lyo.Ledger;

/// <summary>Double-entry journal. Posted journals are immutable; voiding writes a reversing journal.</summary>
[EntityRefLogicalType("LedgerJournal")]
public sealed class LedgerJournalRecord
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }

    /// <summary>Tenant scope.</summary>
    public Guid? TenantId { get; set; }

    /// <summary>Lifecycle.</summary>
    public LedgerJournalStatus Status { get; set; }

    /// <summary>Header memo.</summary>
    public string? Memo { get; set; }

    /// <summary>Document that caused this journal (invoice, payment).</summary>
    public EntityRef? Source { get; set; }

    /// <summary>When set, this journal reverses <see cref="VoidOfJournalId" />.</summary>
    public Guid? VoidOfJournalId { get; set; }

    /// <summary>When the journal was posted (UTC).</summary>
    public DateTime? PostedAt { get; set; }

    /// <summary>When the journal was marked void (UTC).</summary>
    public DateTime? VoidedAt { get; set; }

    /// <summary>When the row was created (UTC).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Lines on this journal (populated on get/post).</summary>
    public IReadOnlyList<LedgerJournalLineRecord> Lines { get; set; } = [];
}