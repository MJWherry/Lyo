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

public partial class ReportManagement
{
    /// <summary>Base route for reporting endpoints (for example "Reporting").</summary>
    [Parameter]
    [EditorRequired]
    public string BaseRoute { get; set; } = "Reporting";

    /// <summary>
    /// Host callback to download a persisted output by opaque file id (usually FileStorage). The second argument is the preferred download file name
    /// (<c>OriginalFileName</c>). When null, download actions show a snackbar that no download handler is configured.
    /// </summary>
    [Parameter]
    public Func<Guid, string?, CancellationToken, Task>? DownloadFileAsync { get; set; }

    /// <summary>Host callback returning a browser URL to view HTML/PDF output. When null, the View action is disabled.</summary>
    [Parameter]
    public Func<Guid, CancellationToken, Task<string?>>? ViewFileUrlAsync { get; set; }

    /// <summary>Starting tab: definitions or generations.</summary>
    [Parameter]
    public string? InitialTab { get; set; }

    [Parameter]
    public Guid? DefinitionId { get; set; }

    [Parameter]
    public Guid? GenerationId { get; set; }

    /// <summary>When set, Definitions shows a Design action that opens the composition workbench.</summary>
    [Parameter]
    public EventCallback<Guid> Design { get; set; }

    private int _tabIndex;
    private bool _tabInitialized;

    protected override void OnParametersSet()
    {
        if (_tabInitialized)
            return;

        if (DefinitionId is null && GenerationId is null && string.IsNullOrWhiteSpace(InitialTab))
            return;

        _tabIndex = ResolveTabIndex();
        _tabInitialized = true;
    }

    private int ResolveTabIndex()
    {
        if (GenerationId.HasValue)
            return 1;

        if (DefinitionId.HasValue)
            return 0;

        return InitialTab?.Trim().ToLowerInvariant() switch {
            "generations" or "generation" => 1,
            var _ => 0
        };
    }

    private async Task OnGenerated(ReportGenerationRes gen)
    {
        _tabIndex = 1;
        var parameters = new DialogParameters<ReportGenerationView> {
            { d => d.Generation, gen },
            { d => d.DownloadFileAsync, DownloadFileAsync },
            { d => d.ViewFileUrlAsync, ViewFileUrlAsync },
            { d => d.LoadReportDataAsync, ReportGenerationDataLoader.Create(ApiClient, $"{BaseRoute.TrimEnd('/')}/Generation") },
            { d => d.OpenOutput, true }
        };
        await DialogService.ShowAsync<ReportGenerationView>("Report Generation", parameters, LyoDialogPresets.Medium);
    }
}
