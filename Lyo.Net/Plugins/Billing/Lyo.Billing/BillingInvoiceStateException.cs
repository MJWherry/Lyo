namespace Lyo.Billing;

/// <summary>Raised when issue, void, or pay is called in an illegal status.</summary>
public sealed class BillingInvoiceStateException(string message)
    : Exception(message);