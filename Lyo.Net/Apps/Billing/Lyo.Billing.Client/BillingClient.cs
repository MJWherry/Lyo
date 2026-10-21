using Lyo.Api.Client;
using Lyo.Exceptions;

namespace Lyo.Billing.Client;

/// <summary>Typed facade over the Lyo Billing API. Sit it on any <see cref="IApiClient" /> implementation.</summary>
public sealed class BillingClient : IBillingClient
{
    private readonly IApiClient _api;
    private readonly string? _routePrefix;

    /// <summary>Constructs the client. Null <paramref name="options" /> keeps routes relative to the inner client's base address.</summary>
    public BillingClient(IApiClient apiClient, BillingClientOptions? options = null)
    {
        ArgumentHelpers.ThrowIfNull(apiClient);
        _api = apiClient;
        _routePrefix = options?.RoutePrefix;
    }

    /// <inheritdoc />
    public Task<InvoiceRecord> CreateFromChargesAsync(CreateInvoiceFromChargesRequest request, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(request);
        return _api.PostAsAsync<CreateInvoiceFromChargesRequest, InvoiceRecord>(Route("Billing/Invoices/from-charges"), request, ct: ct);
    }

    /// <inheritdoc />
    public Task<InvoiceRecord> CreateDraftAsync(CreateDraftInvoiceRequest request, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(request);
        return _api.PostAsAsync<CreateDraftInvoiceRequest, InvoiceRecord>(Route("Billing/Invoices/draft"), request, ct: ct);
    }

    /// <inheritdoc />
    public Task<InvoiceLineRecord> AddLineAsync(Guid invoiceId, InvoiceLineRecord line, Guid? tenantId = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(line);
        return _api.PostAsAsync<InvoiceLineRecord, InvoiceLineRecord>(WithTenant($"Billing/Invoices/{invoiceId:D}/lines", tenantId), line, ct: ct);
    }

    /// <inheritdoc />
    public async Task RemoveLineAsync(Guid invoiceId, Guid lineId, Guid? tenantId = null, CancellationToken ct = default)
        => await _api.DeleteAsAsync<object>(WithTenant($"Billing/Invoices/{invoiceId:D}/lines/{lineId:D}", tenantId), ct: ct).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task DeleteDraftAsync(Guid invoiceId, Guid? tenantId = null, CancellationToken ct = default)
        => await _api.DeleteAsAsync<object>(WithTenant($"Billing/Invoices/{invoiceId:D}/draft", tenantId), ct: ct).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ChargeRecord>> ListUnbilledChargesAsync(Guid clientId, Guid? tenantId = null, CancellationToken ct = default)
    {
        var rows = await _api.GetAsAsync<List<ChargeRecord>>(WithTenant($"Billing/Clients/{clientId:D}/unbilled-charges", tenantId), ct: ct).ConfigureAwait(false);
        return rows ?? [];
    }

    /// <inheritdoc />
    public async Task<BillingSummaryRecord> GetSummaryAsync(Guid? tenantId = null, CancellationToken ct = default)
        => await _api.GetAsAsync<BillingSummaryRecord>(WithTenant("Billing/Summary", tenantId), ct: ct).ConfigureAwait(false) ?? new BillingSummaryRecord();

    /// <inheritdoc />
    public Task<InvoiceRecord?> GetInvoiceAsync(Guid invoiceId, Guid? tenantId = null, CancellationToken ct = default)
        => _api.GetAsAsync<InvoiceRecord>(WithTenant($"Billing/Invoices/{invoiceId:D}/record", tenantId), ct: ct);

    /// <inheritdoc />
    public Task<InvoiceRecord> IssueAsync(Guid invoiceId, Guid? tenantId = null, CancellationToken ct = default)
        => _api.PostAsAsync<InvoiceRecord>(WithTenant($"Billing/Invoices/{invoiceId:D}/issue", tenantId), ct: ct);

    /// <inheritdoc />
    public Task<InvoiceRecord> VoidAsync(Guid invoiceId, Guid? tenantId = null, CancellationToken ct = default)
        => _api.PostAsAsync<InvoiceRecord>(WithTenant($"Billing/Invoices/{invoiceId:D}/void", tenantId), ct: ct);

    /// <inheritdoc />
    public Task<PaymentRecord> PayAsync(Guid invoiceId, RecordInvoicePaymentRequest request, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(request);
        return _api.PostAsAsync<RecordInvoicePaymentRequest, PaymentRecord>(Route($"Billing/Invoices/{invoiceId:D}/pay"), request, ct: ct);
    }

    /// <inheritdoc />
    public string InvoicePdfHref(Guid invoiceId)
    {
        var path = Route($"Reporting/Invoices/{invoiceId:D}/pdf");
        var baseAddress = _api.GetClient().BaseAddress;
        if (baseAddress is null)
            return path.StartsWith("'") ? path : "/" + path;

        return new Uri(baseAddress, path).ToString();
    }

    private string Route(string relativePath) => ApiRouteBuilder.Build(_routePrefix, relativePath);

    private string WithTenant(string relativePath, Guid? tenantId)
    {
        var route = Route(relativePath);
        return tenantId is { } id && id != Guid.Empty ? $"{route}?tenantId={id:D}" : route;
    }
}