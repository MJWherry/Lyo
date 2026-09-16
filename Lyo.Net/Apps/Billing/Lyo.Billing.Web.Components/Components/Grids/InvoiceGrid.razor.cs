using Lyo.Api.Client;
using Lyo.Api.Models.Common.Response;
using Lyo.Query.Models.Builders;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Common.Request;
using Lyo.Query.Models.Enums;
using Lyo.Web.Components.DataGrid;
using Lyo.Web.Components.Dialog;
using Lyo.Web.Components.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace Lyo.Billing.Web.Components.Components.Grids;

public partial class InvoiceGrid
{
    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    [Parameter]
    public Guid? ClientId { get; set; }

    [Parameter]
    public EventCallback OnCompose { get; set; }

    private LyoDataGridProjected? _dataGrid;

    private readonly List<FilterPropertyDefinition> _propertyDefinitions =
    [
        new("Number"),
        FilterPropertyDefinition.FromEnum("Status", BillingColorHelper.EnumFilterValues<InvoiceStatus>())
    ];

    private void ApplyQuery(ProjectionQueryReqBuilder q)
    {
        if (ClientId is { } clientId)
            q.AddWhere(new ConditionClause("ClientId", ComparisonOperatorEnum.Equals, clientId));
    }

    private async Task OpenAsync(object? item)
    {
        if (!TryGetId(item, out var id))
            return;
        var parameters = new DialogParameters<InvoiceDetailView> {
            { d => d.ApiClient, ApiClient },
            { d => d.InvoiceId, id }
        };
        var dialog = await DialogService.ShowAsync<InvoiceDetailView>("Invoice", parameters, LyoDialogPresets.Large);
        await dialog.Result;
        await RefreshAsync();
    }

    private async Task DeleteDraftAsync(object? item)
    {
        if (!TryGetId(item, out var id) || !IsDraft(item))
            return;
        var number = ProjectedValueHelper.GetDisplayValue(item, "Number");
        var subject = string.IsNullOrWhiteSpace(number) ? "this draft invoice" : $"draft {number}";
        if (!await DialogService.ConfirmDeleteAsync(subject, "Reserved charges are unbilled again."))
            return;
        try {
            await Billing.DeleteDraftAsync(id);
            Snackbar.Add("Draft deleted.", Severity.Info);
            await RefreshAsync();
        }
        catch (Exception ex) {
            Snackbar.Add(ex.Message, Severity.Error);
        }
    }

    private async Task VoidAsync(object? item)
    {
        if (!TryGetId(item, out var id) || !CanVoid(item))
            return;
        var number = ProjectedValueHelper.GetDisplayValue(item, "Number");
        if (!await DialogService.ConfirmAsync("Void invoice", $"Void {(string.IsNullOrWhiteSpace(number) ? "this invoice" : number)}? This posts a reversing journal.", "Void"))
            return;
        try {
            await Billing.VoidAsync(id);
            Snackbar.Add("Invoice voided.", Severity.Info);
            await RefreshAsync();
        }
        catch (Exception ex) {
            Snackbar.Add(ex.Message, Severity.Error);
        }
    }

    private async Task BulkPdfAsync()
    {
        var selected = await LoadSelectedAsync();
        var eligible = selected.Where(i => CanPdf((InvoiceStatus)i.Status)).ToList();
        var skipped = selected.Count - eligible.Count;
        if (eligible.Count == 0) {
            Snackbar.Add(selected.Count == 0 ? "Select at least one invoice." : "No issued invoices in the selection.", Severity.Warning);
            return;
        }

        var opened = 0;
        foreach (var invoice in eligible) {
            await Js.InvokeVoidAsync("open", Billing.InvoicePdfHref(invoice.Id), "_blank");
            opened++;
        }

        var suffix = skipped > 0 ? $", {skipped} skipped" : "";
        Snackbar.Add($"Opened {opened} PDF(s){suffix}. Allow popups if some did not appear.", Severity.Info);
    }

    private async Task BulkVoidAsync()
    {
        var selected = await LoadSelectedAsync();
        var eligible = selected.Where(i => CanVoid((InvoiceStatus)i.Status)).ToList();
        var skipped = selected.Count - eligible.Count;
        if (eligible.Count == 0) {
            Snackbar.Add(selected.Count == 0 ? "Select at least one invoice." : "No issued invoices in the selection.", Severity.Warning);
            return;
        }

        if (!await DialogService.ConfirmAsync("Void invoices", $"Void {eligible.Count} invoice(s)? This posts a reversing journal.", "Void"))
            return;

        var ok = 0;
        foreach (var invoice in eligible) {
            try {
                await Billing.VoidAsync(invoice.Id);
                ok++;
            }
            catch (Exception ex) {
                Snackbar.Add(ex.Message, Severity.Error);
            }
        }

        var suffix = skipped > 0 ? $", {skipped} skipped" : "";
        Snackbar.Add($"{ok} voided{suffix}.", Severity.Info);
        await RefreshAsync();
    }

    private async Task BulkDeleteDraftsAsync()
    {
        var selected = await LoadSelectedAsync();
        var eligible = selected.Where(i => (InvoiceStatus)i.Status == InvoiceStatus.Draft).ToList();
        var skipped = selected.Count - eligible.Count;
        if (eligible.Count == 0) {
            Snackbar.Add(selected.Count == 0 ? "Select at least one invoice." : "No drafts in the selection.", Severity.Warning);
            return;
        }

        if (!await DialogService.ConfirmDeleteAsync($"{eligible.Count} draft invoice(s)", "Reserved charges are unbilled again."))
            return;

        var ok = 0;
        foreach (var invoice in eligible) {
            try {
                await Billing.DeleteDraftAsync(invoice.Id);
                ok++;
            }
            catch (Exception ex) {
                Snackbar.Add(ex.Message, Severity.Error);
            }
        }

        var suffix = skipped > 0 ? $", {skipped} skipped" : "";
        Snackbar.Add($"{ok} draft(s) deleted{suffix}.", Severity.Info);
        await RefreshAsync();
    }

    private async Task<IReadOnlyList<InvoiceGridLookup>> LoadSelectedAsync()
    {
        var keys = _dataGrid?.SelectedKeys;
        if (keys is not { Count: > 0 })
            return [];

        var res = await ApiClient.QueryProjectAsync<ProjectedQueryRes<InvoiceGridLookup>>(
            "Billing/Invoices",
            new ProjectionQueryReq {
                Keys = [.. keys],
                Amount = keys.Count,
                Select = ["Id", "Status", "Number"]
            });
        return res?.Items ?? [];
    }

    private bool IsDraft(object? item) => TryGetStatus(item, out var status) && status == InvoiceStatus.Draft;

    private bool CanVoid(object? item) => TryGetStatus(item, out var status) && CanVoid(status);

    private bool CanPdf(object? item) => TryGetStatus(item, out var status) && CanPdf(status);

    private static bool CanVoid(InvoiceStatus status) => status is InvoiceStatus.Issued or InvoiceStatus.PartiallyPaid;

    private static bool CanPdf(InvoiceStatus status) => status is InvoiceStatus.Issued or InvoiceStatus.PartiallyPaid or InvoiceStatus.Paid;

    private string? PdfHref(object? item)
        => TryGetId(item, out var id) ? Billing.InvoicePdfHref(id) : null;

    private static bool TryGetId(object? item, out Guid id)
        => ProjectedValueHelper.TryGetGuid(ProjectedValueHelper.GetValue(item, "Id"), out id);

    private static bool TryGetStatus(object? item, out InvoiceStatus status)
    {
        status = default;
        var raw = ProjectedValueHelper.GetValue(item, "Status");
        if (raw is InvoiceStatus typed) {
            status = typed;
            return true;
        }

        if (ProjectedValueHelper.TryGetInt64(raw, out var n) && Enum.IsDefined(typeof(InvoiceStatus), (int)n)) {
            status = (InvoiceStatus)n;
            return true;
        }

        var text = ProjectedValueHelper.GetDisplayValue(item, "Status");
        return Enum.TryParse(text, ignoreCase: true, out status);
    }

    /// <summary>Reloads the grid.</summary>
    public Task RefreshAsync() => _dataGrid?.RefreshData() ?? Task.CompletedTask;

    private sealed class InvoiceGridLookup
    {
        public Guid Id { get; set; }

        public int Status { get; set; }

        public string? Number { get; set; }
    }
}
