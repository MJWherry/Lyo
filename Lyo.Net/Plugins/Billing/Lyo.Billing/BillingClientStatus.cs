namespace Lyo.Billing;

/// <summary>Lifecycle of a bill-to client row in schema <c>billing</c>.</summary>
public enum BillingClientStatus
{
    /// <summary>Not yet billed; still being qualified.</summary>
    Prospect = 0,

    /// <summary>Open for new invoices.</summary>
    Active = 1,

    /// <summary>Temporarily not billed.</summary>
    OnHold = 2,

    /// <summary>No longer billed, row kept.</summary>
    Inactive = 3,

    /// <summary>Hidden from default lists.</summary>
    Archived = 4
}
