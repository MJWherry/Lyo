namespace Lyo.Ledger;

/// <summary>Well-known chart-of-accounts codes seeded per tenant.</summary>
public static class LedgerAccountCodes
{
    /// <summary>Cash / bank (debit balance).</summary>
    public const string Cash = "1000";

    /// <summary>Accounts receivable (debit balance).</summary>
    public const string AccountsReceivable = "1100";

    /// <summary>Tax payable (credit balance).</summary>
    public const string TaxPayable = "2200";

    /// <summary>Revenue (credit balance).</summary>
    public const string Revenue = "4000";
}