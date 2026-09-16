namespace Lyo.Billing;

/// <summary>Kind of unbilled work.</summary>
public enum ChargeKind
{
    /// <summary>Time / professional services.</summary>
    Time = 0,

    /// <summary>Reimbursable or billed expense.</summary>
    Expense = 1,

    /// <summary>Product or license line.</summary>
    Product = 2,

    /// <summary>Anything else.</summary>
    Other = 3
}
