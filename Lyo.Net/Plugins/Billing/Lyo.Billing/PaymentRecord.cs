namespace Lyo.Billing;

/// <summary>Payment against an issued invoice. Posts Dr Cash / Cr AR.</summary>
public sealed class PaymentRecord
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }

    /// <summary>Tenant scope.</summary>
    public Guid? TenantId { get; set; }

    /// <summary>Invoice paid.</summary>
    public Guid InvoiceId { get; set; }

    /// <summary>Amount in the invoice currency.</summary>
    public decimal Amount { get; set; }

    /// <summary>ISO 4217.</summary>
    public string Currency { get; set; } = "USD";

    /// <summary>Collection method.</summary>
    public PaymentMethod Method { get; set; }

    /// <summary>Check number, wire ref, collector reference.</summary>
    public string? Reference { get; set; }

    /// <summary>Posted cash/AR journal.</summary>
    public Guid? JournalId { get; set; }

    /// <summary>When the payment was recorded (UTC).</summary>
    public DateTime ReceivedAt { get; set; }
}