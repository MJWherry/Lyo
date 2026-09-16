namespace Lyo.Billing;

/// <summary>How a payment was collected. Stripe is out of scope.</summary>
public enum PaymentMethod
{
    /// <summary>Unspecified.</summary>
    Other = 0,

    /// <summary>Check.</summary>
    Check = 1,

    /// <summary>Wire / ACH.</summary>
    Wire = 2,

    /// <summary>Cash.</summary>
    Cash = 3,

    /// <summary>Internal workbench collector (Phase 4).</summary>
    Internal = 4
}
