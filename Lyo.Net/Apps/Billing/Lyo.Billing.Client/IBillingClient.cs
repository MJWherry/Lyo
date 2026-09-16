namespace Lyo.Billing.Client;

/// <summary>Typed HTTP client for the Lyo Billing API. PDF hrefs resolve against the inner client's base address.</summary>
public interface IBillingClient
{
    /// <summary>Creates a draft invoice from unbilled charges.</summary>
    Task<InvoiceRecord> CreateFromChargesAsync(CreateInvoiceFromChargesRequest request, CancellationToken ct = default);

    /// <summary>Creates an empty draft invoice.</summary>
    Task<InvoiceRecord> CreateDraftAsync(CreateDraftInvoiceRequest request, CancellationToken ct = default);

    /// <summary>Adds a line to a draft invoice.</summary>
    Task<InvoiceLineRecord> AddLineAsync(Guid invoiceId, InvoiceLineRecord line, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Removes a draft line.</summary>
    Task RemoveLineAsync(Guid invoiceId, Guid lineId, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Deletes a draft invoice and unreserves its charges.</summary>
    Task DeleteDraftAsync(Guid invoiceId, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Unbilled charges for a client.</summary>
    Task<IReadOnlyList<ChargeRecord>> ListUnbilledChargesAsync(Guid clientId, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Dashboard counts and totals.</summary>
    Task<BillingSummaryRecord> GetSummaryAsync(Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Loads an invoice and its lines.</summary>
    Task<InvoiceRecord?> GetInvoiceAsync(Guid invoiceId, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Issues a draft invoice.</summary>
    Task<InvoiceRecord> IssueAsync(Guid invoiceId, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Voids an unpaid issued invoice.</summary>
    Task<InvoiceRecord> VoidAsync(Guid invoiceId, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Records a payment against an issued invoice.</summary>
    Task<PaymentRecord> PayAsync(Guid invoiceId, RecordInvoicePaymentRequest request, CancellationToken ct = default);

    /// <summary>Absolute or root-relative href for <c>GET /Reporting/Invoices/{id}/pdf</c>.</summary>
    string InvoicePdfHref(Guid invoiceId);
}
