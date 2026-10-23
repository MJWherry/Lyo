namespace Lyo.Ledger;

/// <summary>Raised when a journal is posted with unequal debit and credit totals.</summary>
public sealed class LedgerUnbalancedJournalException(string message)
    : Exception(message);