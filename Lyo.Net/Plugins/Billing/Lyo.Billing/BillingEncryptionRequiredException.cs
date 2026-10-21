namespace Lyo.Billing;

/// <summary>Raised when a tax id or bank/remittance secret is saved but no cipher is registered.</summary>
public sealed class BillingEncryptionRequiredException(string message)
    : Exception(message);