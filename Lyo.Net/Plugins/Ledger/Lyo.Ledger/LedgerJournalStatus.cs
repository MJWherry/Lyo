namespace Lyo.Ledger;

/// <summary>Lifecycle of a journal. Posted rows are immutable; voiding writes a reversing journal.</summary>
public enum LedgerJournalStatus
{
    /// <summary>Editable; not in the books.</summary>
    Draft = 0,

    /// <summary>Balanced and immutable.</summary>
    Posted = 1,

    /// <summary>Reversed by another posted journal.</summary>
    Voided = 2
}