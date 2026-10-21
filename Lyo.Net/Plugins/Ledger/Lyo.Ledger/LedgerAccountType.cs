namespace Lyo.Ledger;

/// <summary>Normal balance classification for a ledger account.</summary>
public enum LedgerAccountType
{
    /// <summary>Debit-normal: cash, AR, expenses.</summary>
    Asset = 0,

    /// <summary>Credit-normal: tax payable, AP.</summary>
    Liability = 1,

    /// <summary>Credit-normal: capital.</summary>
    Equity = 2,

    /// <summary>Credit-normal: sales.</summary>
    Revenue = 3,

    /// <summary>Debit-normal: COGS, opex.</summary>
    Expense = 4
}