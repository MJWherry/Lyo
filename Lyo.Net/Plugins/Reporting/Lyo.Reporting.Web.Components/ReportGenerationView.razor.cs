using Lyo.Api.Models.Common.Response;
using Lyo.Parameters;
using Lyo.Reporting.Client;
using Lyo.Web.Components;
using Lyo.Web.Primitives;
using Lyo.Web.Components.DataGrid;
using Lyo.Web.Components.Dialog;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace Lyo.Reporting.Web.Components;

public partial class ReportGenerationView
{
    [Parameter]
    [EditorRequired]
    public ReportGenerationRes Generation { get; set; } = null!;

    [Parameter]
    public Func<Guid, string?, CancellationToken, Task>? DownloadFileAsync { get; set; }

    [Parameter]
    public Func<Guid, CancellationToken, Task<string?>>? ViewFileUrlAsync { get; set; }

    /// <summary>When true, opens the report output (tabular preview or view URL) after the dialog first paints.</summary>
    [Parameter]
    public bool OpenOutput { get; set; }

    /// <summary>
    /// Reads a generation's composition JSON back from the API. Required when <see cref="Generation" /> came from a generate or rerun response, which omit
    /// <see cref="ReportGenerationRes.ReportDataJson" />. Build one with <see cref="ReportGenerationDataLoader.Create" />.
    /// </summary>
    [Parameter]
    public Func<Guid, CancellationToken, Task<string?>>? LoadReportDataAsync { get; set; }

    private bool _outputOpened;

    private string? _loadedReportDataJson;

    private string DurationText => ReportColorHelper.FormatDurationFromDates(Generation.StartedTimestamp, Generation.FinishedTimestamp);

    private IReadOnlyList<ILyoParameterValue> Parameters => (Generation.Parameters ?? []).Cast<ILyoParameterValue>().ToList();

    private bool CanView
        => Generation.Format is ReportFormat.Csv or ReportFormat.Xlsx
            ? !string.IsNullOrWhiteSpace(Generation.ReportDataJson) || LoadReportDataAsync is not null
            : Generation.OutputFileId is not null && ViewFileUrlAsync is not null;

    private bool CanDownload => Generation.OutputFileId is not null && DownloadFileAsync is not null;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || !OpenOutput || _outputOpened || !CanView)
            return;

        _outputOpened = true;
        await ViewAsync();
    }

    private async Task ViewAsync()
    {
        if (Generation.Format is ReportFormat.Csv or ReportFormat.Xlsx) {
            try {
                var reportDataJson = await ResolveReportDataJsonAsync();
                if (string.IsNullOrWhiteSpace(reportDataJson)) {
                    Snackbar.Add("Report data is unavailable for this generation.", Severity.Warning);
                    return;
                }

                var sheets = ReportGridDataTableMapper.FromReportDataJson(reportDataJson);
                if (Generation.Format == ReportFormat.Csv && sheets.Count > 1)
                    sheets = [sheets[0]];

                if (sheets.Count == 0) {
                    Snackbar.Add("No grids found to preview.", Severity.Warning);
                    return;
                }

                var title = string.IsNullOrWhiteSpace(Generation.OriginalFileName)
                    ? $"{Generation.Format} preview"
                    : Generation.OriginalFileName!;
                var parameters = new DialogParameters<ReportTabularPreviewDialog> { { d => d.Sheets, sheets } };
                await DialogService.ShowAsync<ReportTabularPreviewDialog>(title, parameters, LyoDialogPresets.Large);
            }
            catch (Exception ex) {
                Snackbar.Add($"Preview failed: {ex.Message}", Severity.Error);
            }

            return;
        }

        if (Generation.OutputFileId is not Guid fileId || ViewFileUrlAsync is null) {
            Snackbar.Add("View is unavailable for this generation.", Severity.Info);
            return;
        }

        var url = await ViewFileUrlAsync(fileId, CancellationToken.None);
        if (string.IsNullOrWhiteSpace(url)) {
            Snackbar.Add("No view URL available.", Severity.Warning);
            return;
        }

        await JsRuntime.InvokeVoidAsync("open", url, "_blank");
    }

    private async Task<string?> ResolveReportDataJsonAsync()
    {
        if (!string.IsNullOrWhiteSpace(Generation.ReportDataJson))
            return Generation.ReportDataJson;

        if (!string.IsNullOrWhiteSpace(_loadedReportDataJson) || LoadReportDataAsync is null)
            return _loadedReportDataJson;

        _loadedReportDataJson = await LoadReportDataAsync(Generation.Id, CancellationToken.None);
        return _loadedReportDataJson;
    }

    private async Task DownloadAsync()
    {
        if (Generation.OutputFileId is not Guid fileId || DownloadFileAsync is null) {
            Snackbar.Add("Download is unavailable for this generation.", Severity.Info);
            return;
        }

        await DownloadFileAsync(fileId, Generation.OriginalFileName, CancellationToken.None);
        Snackbar.Add("Download started.", Severity.Success);
    }
}
