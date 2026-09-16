namespace Lyo.Billing;

/// <summary>How an address is used on a billing client or contact.</summary>
public enum BillingAddressUsage
{
    /// <summary>Unspecified.</summary>
    Other = 0,

    /// <summary>Bill-to.</summary>
    Billing = 1,

    /// <summary>Ship-to.</summary>
    Shipping = 2,

    /// <summary>Mailing.</summary>
    Mailing = 3
}
