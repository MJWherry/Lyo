namespace Lyo.Payments;

/// <summary>Collects a payment against an invoice. Stripe is out of scope; hosts register <see cref="InternalPaymentCollector" /> for workbenches.</summary>
public interface IPaymentCollector
{
    /// <summary>Collector kind (for example <c>Internal</c>).</summary>
    string Kind { get; }

    /// <summary>Attempts to collect <paramref name="request" />. Does not post the ledger; the host calls Billing <c>RecordPaymentAsync</c> on success.</summary>
    Task<PaymentCollectionResult> CollectAsync(PaymentCollectionRequest request, CancellationToken ct = default);
}
