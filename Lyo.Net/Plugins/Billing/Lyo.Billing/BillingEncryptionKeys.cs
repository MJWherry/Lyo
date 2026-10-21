namespace Lyo.Billing;

/// <summary>Keyed <c>IEncryptionService</c> names used to encrypt invoice snapshots and remittance at rest.</summary>
public static class BillingEncryptionKeys
{
    /// <summary>Key-store / encryption service key for invoice tax snapshots and biller bank/remittance.</summary>
    public const string Fields = "billing-fields";
}