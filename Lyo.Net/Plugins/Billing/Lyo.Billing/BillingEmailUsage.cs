namespace Lyo.Billing;

/// <summary>How an email is used on a billing client or contact.</summary>
public enum BillingEmailUsage
{
    /// <summary>Unspecified.</summary>
    Other = 0,

    /// <summary>Work / general.</summary>
    Work = 1,

    /// <summary>Invoices and remittance.</summary>
    Billing = 2
}
