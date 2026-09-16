using Lyo.Exceptions;

namespace Lyo.Payments;

/// <summary>Workbench collector that always succeeds. No card network, no Stripe.</summary>
public sealed class InternalPaymentCollector : IPaymentCollector
{
    /// <inheritdoc />
    public string Kind => "Internal";

    /// <inheritdoc />
    public Task<PaymentCollectionResult> CollectAsync(PaymentCollectionRequest request, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(request);
        if (request.InvoiceId == Guid.Empty)
            throw new ArgumentException("Invoice id is required.", nameof(request));
        if (request.Amount <= 0)
            throw new ArgumentException("Amount must be positive.", nameof(request));

        ct.ThrowIfCancellationRequested();
        var trimmed = request.Reference?.Trim();
        var reference = string.IsNullOrEmpty(trimmed)
            ? $"INT-{request.InvoiceId.ToString("N").Substring(0, 8).ToUpperInvariant()}"
            : trimmed;
        return Task.FromResult(new PaymentCollectionResult { Succeeded = true, Reference = reference });
    }
}
