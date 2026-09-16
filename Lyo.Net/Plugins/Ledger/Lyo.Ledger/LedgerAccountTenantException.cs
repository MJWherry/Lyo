namespace Lyo.Ledger;

/// <summary>Raised when a journal line references an account that does not belong to the journal tenant.</summary>
public sealed class LedgerAccountTenantException(string message) : Exception(message);
