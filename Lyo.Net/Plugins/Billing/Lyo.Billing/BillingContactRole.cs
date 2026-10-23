namespace Lyo.Billing;

/// <summary>Role of a party attached to a billing client.</summary>
public enum BillingContactRole
{
    /// <summary>Unspecified.</summary>
    Other = 0,

    /// <summary>Receives invoices and payment notices.</summary>
    Billing = 1,

    /// <summary>Technical or operational contact.</summary>
    Technical = 2,

    /// <summary>Executive or decision-maker.</summary>
    Executive = 3
}