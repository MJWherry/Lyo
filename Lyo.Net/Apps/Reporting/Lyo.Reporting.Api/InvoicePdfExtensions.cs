using System.Globalization;
using System.Text.Json;
using Lyo.Api.Models.Error;
using Lyo.Billing;
using Lyo.Common.Metadata.Records;
using Lyo.Reporting.Models;
using Lyo.Reporting.Models.Composition;
using Lyo.Reporting.Models.Controls;
using Lyo.Reporting.Models.Enums;
using Lyo.Reporting.Models.Models;
using Lyo.Reporting.Models.Request;
using Lyo.Reporting.Postgres;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using ApiErrorCodes = Lyo.Api.Models.Constants.ApiErrorCodes;

namespace Lyo.Reporting.Api;

/// <summary>Invoice PDF for issued billing documents. Lives on the Reporting host; Billing plugins do not reference Reporting.</summary>
public static class InvoicePdfExtensions
{
    /// <summary>
    /// Maps <c>GET /Reporting/Invoices/{id}/pdf</c>. Generates the Invoice composition as PDF when <see cref="IBillingStore" /> is registered.
    /// When a download factory is configured and generation stored an output file, the PDF bytes are streamed; otherwise the generation JSON is returned.
    /// </summary>
    public static WebApplication MapBillingInvoicePdf(this WebApplication app, ReportingApiOptions? options = null)
    {
        options ??= new();
        var endpoint = app.MapGet(
            "/Reporting/Invoices/{id:guid}/pdf",
            async (Guid id, HttpContext http, IServiceProvider services, CancellationToken ct) => {
                var billing = services.GetService<IBillingStore>();
                if (billing is null)
                    return Results.StatusCode(StatusCodes.Status501NotImplemented);

                var invoice = await billing.GetInvoiceByIdAsync(id, ct: ct).ConfigureAwait(false);
                if (invoice is null)
                    return Results.NotFound();
                if (invoice.Status is InvoiceStatus.Draft or InvoiceStatus.Void)
                    return Results.BadRequest("Draft and void invoices cannot be rendered.");

                var reports = services.GetRequiredService<ReportService>();
                var req = InvoicePdfComposition.CreateRequest(invoice);
                Extensions.StampCreatedBy(req, http, options);

                try {
                    var result = await reports.GenerateAsync(req, ct: ct).ConfigureAwait(false);
                    if (options.DownloadStreamFactory is { } downloadFactory
                        && result.OutputFileId is Guid outputFileId
                        && result.Status == ReportGenerationStatus.Succeeded) {
                        var stream = await downloadFactory(
                                new() {
                                    GenerationId = result.Id,
                                    OutputFileId = outputFileId,
                                    ContentType = result.ContentType,
                                    FileName = result.OriginalFileName,
                                    PathPrefix = result.PathPrefix,
                                    Services = services
                                }, ct)
                            .ConfigureAwait(false);
                        if (stream is not null)
                            return Results.Stream(stream, result.ContentType ?? FileTypeInfo.Pdf.MimeType, result.OriginalFileName);
                    }

                    return Results.Ok(result);
                }
                catch (ReportValidationException ex) {
                    throw ApiErrorException.From(LyoProblemDetails.FromCode(ApiErrorCodes.InvalidRequest, ex.Message));
                }
                catch (ReportBusyException ex) {
                    throw ApiErrorException.From(LyoProblemDetails.FromCode(ApiErrorCodes.ServiceUnavailable, ex.Message));
                }
                catch (OperationCanceledException) when (!ct.IsCancellationRequested) {
                    throw ApiErrorException.From(
                        LyoProblemDetails.FromCode(ApiErrorCodes.GatewayTimeout, "Report generation exceeded the configured GenerationTimeout and was cancelled."));
                }
                catch (OperationCanceledException) {
                    throw ApiErrorException.From(LyoProblemDetails.FromCode(ApiErrorCodes.Cancelled, "The report generation request was cancelled by the caller."));
                }
            }).WithTags("Reporting").WithName("GenerateInvoicePdf");

        endpoint.ApplyEndpointAuth(options.GenerateAuth);
        return app;
    }
}

/// <summary>Binds a live <see cref="InvoiceRecord"/> onto the Invoice composition (formatted money, real totals, no designer sample copy).</summary>
internal static class InvoicePdfComposition
{
    /// <summary>Builds the generate request: live line items, bill-to, and totals (tax omitted when <see cref="InvoiceRecord.TaxAmount"/> is 0).</summary>
    public static GenerateReportReq CreateRequest(InvoiceRecord invoice)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        var report = ReportDesignTemplates.Invoice();
        ApplyLiveValues(report, invoice);
        var lineItems = JsonSerializer.Serialize(invoice.Lines.OrderBy(l => l.LineNumber).Select(l => LineRow(l, invoice.Currency)));
        return new GenerateReportReq {
            ReportDataJson = ReportJson.Serialize(report),
            Format = ReportFormat.Pdf,
            FileName = invoice.Number ?? "invoice",
            Parameters =
            [
                new ReportGenerationParameterReq("InvoiceNo", LyoTypeInfo.String, invoice.Number ?? invoice.Id.ToString()),
                new ReportGenerationParameterReq("Period", LyoTypeInfo.String, invoice.IssuedAt?.ToString("yyyy-MM") ?? ""),
                new ReportGenerationParameterReq("Customer", LyoTypeInfo.String, invoice.BillToName ?? invoice.BillToContactName ?? ""),
                new ReportGenerationParameterReq("IssuedAt", LyoTypeInfo.String, FormatDate(invoice.IssuedAt)),
                new ReportGenerationParameterReq("DueAt", LyoTypeInfo.String, FormatDate(invoice.DueAt)),
                new ReportGenerationParameterReq("LineItems", LyoTypeInfo.JsonArray, lineItems)
            ]
        };
    }

    internal static void ApplyLiveValues(Report<object> report, InvoiceRecord invoice)
    {
        foreach (var block in report.Sections.SelectMany(s => SectionBody.WalkControls(s.Controls)).OfType<Block>()) {
            if (block.ContentType == ContentType.Address)
                block.ListItems = [.. BillToLines(invoice)];
            else if (block.ContentType == ContentType.Totals)
                block.ListItems = [.. TotalsRows(invoice)];
            else if (block.ContentType == ContentType.Notes)
                block.Content = string.IsNullOrWhiteSpace(invoice.Notes) ? null : invoice.Notes;
        }

        foreach (var section in report.Sections) {
            section.Controls.RemoveAll(c => c is Block { ContentType: ContentType.Notes, Content: null or "" });
        }
    }

    internal static Dictionary<string, object?> LineRow(InvoiceLineRecord line, string? currency)
    {
        var title = string.IsNullOrWhiteSpace(line.Title) ? line.Description : line.Title;
        var description = string.IsNullOrWhiteSpace(line.Description) || string.Equals(line.Description, title, StringComparison.Ordinal)
            ? ""
            : line.Description;
        return new Dictionary<string, object?> {
            ["title"] = title,
            ["description"] = description,
            ["qty"] = FormatQty(line.Quantity),
            ["amount"] = FormatMoney(line.Amount, currency)
        };
    }

    internal static List<string> TotalsRows(InvoiceRecord invoice)
    {
        var currency = invoice.Currency;
        var rows = new List<string> { $"Subtotal|{FormatMoney(invoice.Subtotal, currency)}" };
        if (invoice.TaxAmount != 0m)
            rows.Add($"Tax|{FormatMoney(invoice.TaxAmount, currency)}");
        rows.Add($"Total|{FormatMoney(invoice.Total, currency)}");
        return rows;
    }

    internal static List<string> BillToLines(InvoiceRecord invoice)
    {
        var lines = new List<string>();
        Add(invoice.BillToName);
        if (!string.Equals(invoice.BillToContactName, invoice.BillToName, StringComparison.OrdinalIgnoreCase))
            Add(invoice.BillToContactName);
        Add(invoice.BillToLine1);
        Add(invoice.BillToLine2);
        var city = string.Join(
            ", ",
            new[] { invoice.BillToCity, CombineRegionPostal(invoice.BillToRegion, invoice.BillToPostalCode) }.Where(s => !string.IsNullOrWhiteSpace(s)));
        Add(city);
        Add(invoice.BillToCountry);
        return lines;

        void Add(string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                lines.Add(value.Trim());
        }
    }

    internal static string FormatMoney(decimal amount, string? currency)
    {
        var code = string.IsNullOrWhiteSpace(currency) ? "USD" : currency.Trim();
        return string.Equals(code, "USD", StringComparison.OrdinalIgnoreCase)
            ? amount.ToString("C2", CultureInfo.GetCultureInfo("en-US"))
            : string.Create(CultureInfo.InvariantCulture, $"{amount:N2} {code}");
    }

    internal static string FormatQty(decimal quantity)
        => quantity.ToString("0.##", CultureInfo.InvariantCulture);

    private static string FormatDate(DateTime? value)
        => value is { } d ? d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : "";

    private static string? CombineRegionPostal(string? region, string? postal)
    {
        if (string.IsNullOrWhiteSpace(region))
            return string.IsNullOrWhiteSpace(postal) ? null : postal.Trim();
        return string.IsNullOrWhiteSpace(postal) ? region.Trim() : $"{region.Trim()} {postal.Trim()}";
    }
}
