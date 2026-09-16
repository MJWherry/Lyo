using System.ComponentModel.DataAnnotations;

namespace Lyo.Ledger.Postgres.Database;

/// <summary>Journal line (debit or credit).</summary>
public sealed class LedgerJournalLineEntity
{
    [Key]
    public Guid Id { get; set; }

    public Guid JournalId { get; set; }

    public int LineNumber { get; set; }

    public Guid AccountId { get; set; }

    public decimal Debit { get; set; }

    public decimal Credit { get; set; }

    public Guid? ClientId { get; set; }

    [MaxLength(1000)]
    public string? Memo { get; set; }

    public LedgerJournalEntity Journal { get; set; } = null!;

    public LedgerAccountEntity Account { get; set; } = null!;
}
