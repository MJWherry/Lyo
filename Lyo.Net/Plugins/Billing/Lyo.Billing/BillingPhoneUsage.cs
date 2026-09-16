namespace Lyo.Billing;

/// <summary>How a phone is used on a billing client or contact.</summary>
public enum BillingPhoneUsage
{
    /// <summary>Unspecified.</summary>
    Other = 0,

    /// <summary>Mobile.</summary>
    Mobile = 1,

    /// <summary>Work / office.</summary>
    Work = 2,

    /// <summary>Fax.</summary>
    Fax = 3
}
