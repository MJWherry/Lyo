namespace Lyo.Payments;

/// <summary>Input to <see cref="IPaymentCollector.CollectAsync" />.</summary>
public sealed class PaymentCollectionRequest
{
    /// <summary>Invoice being paid.</summary>
    public Guid InvoiceId { get; set; }

    /// <summary>Amount in <see cref="Currency" />.</summary>
    public decimal Amount { get; set; }

    /// <summary>ISO 4217 currency.</summary>
    public string Currency { get; set; } = "USD";

    /// <summary>Optional payer reference (check number, wire id).</summary>
    public string? Reference { get; set; }
}
