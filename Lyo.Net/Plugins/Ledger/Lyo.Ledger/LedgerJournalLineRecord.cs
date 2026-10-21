namespace Lyo.Ledger;

/// <summary>One debit or credit on a journal. Amounts use <c>numeric(18,4)</c>.</summary>
public sealed class LedgerJournalLineRecord
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }

    /// <summary>Parent journal.</summary>
    public Guid JournalId { get; set; }

    /// <summary>1-based order on the journal.</summary>
    public int LineNumber { get; set; }

    /// <summary>Account this line posts to.</summary>
    public Guid AccountId { get; set; }

    /// <summary>Debit amount. Zero when this line is a credit.</summary>
    public decimal Debit { get; set; }

    /// <summary>Credit amount. Zero when this line is a debit.</summary>
    public decimal Credit { get; set; }

    /// <summary>Optional client this AR/AP line belongs to. Not a PostgreSQL FK across schemas.</summary>
    public Guid? ClientId { get; set; }

    /// <summary>Optional line memo.</summary>
    public string? Memo { get; set; }
}