namespace Lyo.Ledger;

/// <summary>Raised when a posted or voided journal is mutated instead of being reversed.</summary>
public sealed class LedgerPostedJournalImmutableException(string message) : Exception(message);
