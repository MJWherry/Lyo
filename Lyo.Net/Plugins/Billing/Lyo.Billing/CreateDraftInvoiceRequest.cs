namespace Lyo.Billing;

/// <summary>Body for <c>POST /Billing/Invoices/draft</c>.</summary>
public sealed class CreateDraftInvoiceRequest
{
    /// <summary>Optional tenant.</summary>
    public Guid? TenantId { get; set; }

    /// <summary>Client to invoice.</summary>
    public Guid ClientId { get; set; }

    /// <summary>Tax rate (0.08 = 8%).</summary>
    public decimal TaxRate { get; set; }
}
