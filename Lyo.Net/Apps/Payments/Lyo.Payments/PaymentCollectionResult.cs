namespace Lyo.Payments;

/// <summary>Outcome of <see cref="IPaymentCollector.CollectAsync" />.</summary>
public sealed class PaymentCollectionResult
{
    /// <summary>True when funds were accepted.</summary>
    public bool Succeeded { get; set; }

    /// <summary>Collector reference to store on the payment row.</summary>
    public string? Reference { get; set; }

    /// <summary>Failure reason when <see cref="Succeeded" /> is false.</summary>
    public string? FailureReason { get; set; }
}
