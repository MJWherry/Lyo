using Lyo.Api.Client;
using Lyo.Api.Models.Common.Response;
using Lyo.Parameters;
using Lyo.Query.Models.Common.Request;
using Lyo.Web.Components.DataGrid;
using Lyo.Web.Components.Dialog;
using Lyo.Web.Components.Form;
using Lyo.Web.Components.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace Lyo.Reporting.Web.Components;

public partial class ReportGenerationGrid
{
    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    [Parameter]
    [EditorRequired]
    public string BaseRoute { get; set; } = "Reporting";

    [Parameter]
    public Func<Guid, string?, CancellationToken, Task>? DownloadFileAsync { get; set; }

    [Parameter]
    public Func<Guid, CancellationToken, Task<string?>>? ViewFileUrlAsync { get; set; }

    private string _generationRoute => $"{BaseRoute.TrimEnd('/')}/Generation";

    private LyoDataGridProjected? _dataGrid;

    private readonly List<FilterPropertyDefinition> _propertyDefinitions = [new("Status"), new("Format"), new("CreatedBy", "Created By"), new("ReportDefinition.Name", "Definition")];

    private static readonly string[] _keySelectFields = ["Id", "OutputFileId", "StartedTimestamp", "FinishedTimestamp", "OriginalFileName", "ReportDefinitionId"];

    private static readonly string[] _quickSearchProperties = ["Id", "ReportDefinitionId", "ReportDefinition.Name", "OriginalFileName", "OutputFileId"];


    private static (DateTime? Started, DateTime? Finished) GetTimestamps(object? item)
        => (LyoDateTimeDisplay.ToDateTime(ProjectedValueHelper.GetValue(item, "StartedTimestamp")),
            LyoDateTimeDisplay.ToDateTime(ProjectedValueHelper.GetValue(item, "FinishedTimestamp")));

    private static bool HasOutput(object? item)
    {
        var id = ProjectedValueHelper.GetValue(item, "OutputFileId");
        return ProjectedValueHelper.TryGetGuid(id, out var _);
    }

    private bool CanViewOutput(object? item)
    {
        var formatText = ProjectedValueHelper.GetDisplayValue(item, "Format");
        if (Enum.TryParse<ReportFormat>(formatText, out var format) && format is ReportFormat.Csv or ReportFormat.Xlsx)
            return true;

        return HasOutput(item) && ViewFileUrlAsync is not null;
    }

    private bool CanDownloadOutput(object? item) => HasOutput(item) && DownloadFileAsync is not null;

    private async Task<ReportGenerationRes?> FetchGeneration(object? id)
    {
        if (!ProjectedValueHelper.TryGetGuid(id, out var guid))
            return null;

        var req = new QueryConcreteReq { Keys = [[guid]], Amount = 1, Include = ["Parameters"] };
        var res = await ApiClient.PostAsAsync<QueryConcreteReq, QueryRes<ReportGenerationRes>>($"{_generationRoute}/QueryConcrete", req);
        return res?.Items?.FirstOrDefault();
    }

    private async Task ViewGeneration(object? item)
    {
        var id = ProjectedValueHelper.GetValue(item, "Id");
        var gen = await FetchGeneration(id);
        if (gen is null) {
            Snackbar.Add("Generation not found.", Severity.Warning);
            return;
        }

        var parameters = new DialogParameters<ReportGenerationView> {
            { d => d.Generation, gen },
            { d => d.DownloadFileAsync, DownloadFileAsync },
            { d => d.ViewFileUrlAsync, ViewFileUrlAsync },
            { d => d.LoadReportDataAsync, ReportGenerationDataLoader.Create(ApiClient, _generationRoute) }
        };
        await DialogService.ShowAsync<ReportGenerationView>("Report Generation", parameters, LyoDialogPresets.Medium);
    }

    private async Task ViewOutput(object? item)
    {
        var formatText = ProjectedValueHelper.GetDisplayValue(item, "Format");
        if (Enum.TryParse<ReportFormat>(formatText, out var format) && format is ReportFormat.Csv or ReportFormat.Xlsx) {
            var id = ProjectedValueHelper.GetValue(item, "Id");
            var gen = await FetchGeneration(id);
            if (gen is null) {
                Snackbar.Add("Generation not found.", Severity.Warning);
                return;
            }

            await ShowTabularPreviewAsync(gen);
            return;
        }

        var fileId = ProjectedValueHelper.GetValue(item, "OutputFileId");
        if (!ProjectedValueHelper.TryGetGuid(fileId, out var guid)) {
            Snackbar.Add("No output file on this generation.", Severity.Info);
            return;
        }

        if (ViewFileUrlAsync is null) {
            Snackbar.Add("Host ViewFileUrlAsync is not configured.", Severity.Warning);
            return;
        }

        var url = await ViewFileUrlAsync(guid, CancellationToken.None);
        if (string.IsNullOrWhiteSpace(url)) {
            Snackbar.Add("No view URL available.", Severity.Warning);
            return;
        }

        await JsRuntime.InvokeVoidAsync("open", url, "_blank");
    }

    private async Task ShowTabularPreviewAsync(ReportGenerationRes gen)
    {
        try {
            var sheets = ReportGridDataTableMapper.FromReportDataJson(gen.ReportDataJson);
            if (gen.Format == ReportFormat.Csv && sheets.Count > 1)
                sheets = [sheets[0]];

            if (sheets.Count == 0) {
                Snackbar.Add("No grids found to preview.", Severity.Warning);
                return;
            }

            var title = string.IsNullOrWhiteSpace(gen.OriginalFileName) ? $"{gen.Format} preview" : gen.OriginalFileName!;
            var parameters = new DialogParameters<ReportTabularPreviewDialog> { { d => d.Sheets, sheets } };
            await DialogService.ShowAsync<ReportTabularPreviewDialog>(title, parameters, LyoDialogPresets.Large);
        }
        catch (Exception ex) {
            Snackbar.Add($"Preview failed: {ex.Message}", Severity.Error);
        }
    }

    private async Task DownloadOutput(object? item)
    {
        var fileId = ProjectedValueHelper.GetValue(item, "OutputFileId");
        if (!ProjectedValueHelper.TryGetGuid(fileId, out var guid)) {
            Snackbar.Add("No output file on this generation.", Severity.Info);
            return;
        }

        if (DownloadFileAsync is null) {
            Snackbar.Add("Host DownloadFileAsync is not configured.", Severity.Warning);
            return;
        }

        var fileName = ProjectedValueHelper.GetDisplayValue(item, "OriginalFileName");
        await DownloadFileAsync(guid, string.IsNullOrWhiteSpace(fileName) ? null : fileName, CancellationToken.None);
        Snackbar.Add("Download started.", Severity.Success);
    }

    private async Task DeleteGeneration(object? item)
    {
        var id = ProjectedValueHelper.GetValue(item, "Id");
        if (id == null)
            return;

        var label = ProjectedValueHelper.GetDisplayValue(item, "OriginalFileName");
        if (string.IsNullOrWhiteSpace(label))
            label = LyoIdField.Abbreviate(id.ToString(), LyoIdAbbreviation.Prefix, 8);

        if (!await DialogService.ConfirmDeleteAsync($"'{label}'", "The stored output file is removed too.", "Delete generation"))
            return;

        try {
            await ApiClient.DeleteAsAsync<object>($"{_generationRoute}/{id}");
            Snackbar.Add("Generation deleted", Severity.Success);
            await _dataGrid!.RefreshData();
        }
        catch (Exception ex) {
            Snackbar.Add($"Delete failed: {ex.Message}", Severity.Error);
        }
    }
}
