namespace Lyo.Billing;

/// <summary>Body for <c>POST /Billing/Invoices/{id}/pay</c>.</summary>
public sealed class RecordInvoicePaymentRequest
{
    /// <summary>Optional tenant.</summary>
    public Guid? TenantId { get; set; }

    /// <summary>Payment amount.</summary>
    public decimal Amount { get; set; }

    /// <summary>ISO 4217. Null uses the invoice currency.</summary>
    public string? Currency { get; set; }

    /// <summary>Collection method.</summary>
    public PaymentMethod Method { get; set; }

    /// <summary>Check number, wire ref, collector reference.</summary>
    public string? Reference { get; set; }
}