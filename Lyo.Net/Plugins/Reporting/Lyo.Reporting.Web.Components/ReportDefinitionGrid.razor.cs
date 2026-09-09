using Lyo.Api.Client;
using Lyo.Api.Models.Common.Response;
using Lyo.Parameters;
using Lyo.Query.Models.Common.Request;
using Lyo.Web.Components;
using Lyo.Web.Primitives;
using Lyo.Web.Components.DataGrid;
using Lyo.Web.Components.Dialog;
using Lyo.Web.Components.Form;
using Lyo.Web.Components.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace Lyo.Reporting.Web.Components;

public partial class ReportDefinitionGrid
{
    static ReportDefinitionGrid() => FormatterLyoType.EnsureRegistered();

    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    [Parameter]
    [EditorRequired]
    public string BaseRoute { get; set; } = "Reporting";

    [Parameter]
    public Func<Guid, string?, CancellationToken, Task>? DownloadFileAsync { get; set; }

    /// <summary>Host callback returning a browser URL to view HTML/PDF output. Forwarded when a generation view is opened after a generate.</summary>
    [Parameter]
    public Func<Guid, CancellationToken, Task<string?>>? ViewFileUrlAsync { get; set; }

    /// <summary>Fired after a successful generate so the host can switch tabs. When unset, this grid opens the generation view itself.</summary>
    [Parameter]
    public EventCallback<ReportGenerationRes> Generated { get; set; }

    /// <summary>When set, the grid shows a Design action that opens the composition workbench for that definition.</summary>
    [Parameter]
    public EventCallback<Guid> Design { get; set; }

    private string _definitionRoute => $"{BaseRoute.TrimEnd('/')}/Definition";

    private string _generationRoute => $"{BaseRoute.TrimEnd('/')}/Generation";

    private LyoDataGridProjected? _dataGrid;

    private readonly ProjectedBoolOverrides _active = new("IsActive");

    private readonly List<FilterPropertyDefinition> _propertyDefinitions = [new("Name"), new("GenerationProfileKey", "Profile"), new("IsActive", "Active", LyoTypeInfo.Bool)];

    private bool GetActive(object? item) => _active.Get(item);

    private Task ToggleActiveAsync(object? item) => _active.ToggleAsync(ApiClient, _definitionRoute, item, Snackbar, "Definition");

    private async Task<ReportDefinitionRes?> FetchDefinition(object? id)
    {
        if (!ProjectedValueHelper.TryGetGuid(id, out var guid))
            return null;

        var req = new QueryConcreteReq { Keys = [[guid]], Amount = 1, Include = ["Parameters"] };
        var res = await ApiClient.PostAsAsync<QueryConcreteReq, QueryRes<ReportDefinitionRes>>($"{_definitionRoute}/QueryConcrete", req);
        return res?.Items?.FirstOrDefault();
    }

    private async Task OpenRunDialog()
    {
        var res = await ApiClient.PostAsAsync<QueryConcreteReq, QueryRes<ReportDefinitionRes>>($"{_definitionRoute}/QueryConcrete", new() { Amount = 200, Include = ["Parameters"] });
        var parameters = new DialogParameters<RunReportDialog> { { d => d.Definitions, res?.Items ?? [] }, { d => d.GenerateRoute, $"{_generationRoute}/Generate" }, { d => d.ApiClient, ApiClient } };
        var dialog = await DialogService.ShowAsync<RunReportDialog>("Run Report", parameters, LyoDialogPresets.Medium);
        var result = await dialog.Result;
        if (result is { Canceled: false, Data: ReportGenerationRes gen })
            await AfterGeneratedAsync(gen);
        else if (result is { Canceled: false })
            await _dataGrid!.RefreshData();
    }

    private async Task ViewDefinition(object? item)
    {
        if (item == null)
            return;

        var id = ProjectedValueHelper.GetValue(item, "Id");
        var def = await FetchDefinition(id);
        if (def is null) {
            Snackbar.Add("Definition not found.", Severity.Warning);
            return;
        }

        var parameters = new DialogParameters<ReportDefinitionView> {
            { d => d.Definition, def },
            { d => d.DefinitionRoute, _definitionRoute },
            { d => d.Design, Design }
        };
        await DialogService.ShowAsync<ReportDefinitionView>("Report Definition", parameters, LyoDialogPresets.Medium);
    }

    private Task DesignDefinition(object? item)
    {
        var id = ProjectedValueHelper.GetValue(item, "Id");
        return ProjectedValueHelper.TryGetGuid(id, out var guid) ? Design.InvokeAsync(guid) : Task.CompletedTask;
    }

    private async Task RunDefinition(object? item)
    {
        if (item == null)
            return;

        var id = ProjectedValueHelper.GetValue(item, "Id");
        var def = await FetchDefinition(id);
        if (def is null) {
            Snackbar.Add("Definition not found.", Severity.Warning);
            return;
        }

        var parameters = new DialogParameters<RunReportDialog> {
            { d => d.Definitions, [def] },
            { d => d.SelectedDefinition, def },
            { d => d.GenerateRoute, $"{_generationRoute}/Generate" },
            { d => d.ApiClient, ApiClient }
        };

        var dialog = await DialogService.ShowAsync<RunReportDialog>("Run Report", parameters, LyoDialogPresets.Medium);
        var result = await dialog.Result;
        if (result is { Canceled: false, Data: ReportGenerationRes gen })
            await AfterGeneratedAsync(gen);
    }

    private async Task AfterGeneratedAsync(ReportGenerationRes gen)
    {
        await _dataGrid!.RefreshData();
        if (Generated.HasDelegate)
            await Generated.InvokeAsync(gen);
        else
            await ShowGenerationViewAsync(gen);
    }

    private Task ShowGenerationViewAsync(ReportGenerationRes gen)
    {
        var parameters = new DialogParameters<ReportGenerationView> {
            { d => d.Generation, gen },
            { d => d.DownloadFileAsync, DownloadFileAsync },
            { d => d.ViewFileUrlAsync, ViewFileUrlAsync },
            { d => d.LoadReportDataAsync, ReportGenerationDataLoader.Create(ApiClient, _generationRoute) },
            { d => d.OpenOutput, true }
        };
        return DialogService.ShowAsync<ReportGenerationView>("Report Generation", parameters, LyoDialogPresets.Medium);
    }

    private async Task DeleteDefinition(object? item)
    {
        var id = ProjectedValueHelper.GetValue(item, "Id");
        if (id == null)
            return;

        var name = ProjectedValueHelper.GetDisplayValue(item, "Name");
        if (string.IsNullOrWhiteSpace(name))
            name = LyoIdField.Abbreviate(id.ToString(), LyoIdAbbreviation.Prefix, 8);

        if (!await DialogService.ConfirmDeleteAsync($"'{name}' and all of its generations", "Stored output files are removed too.", "Delete definition"))
            return;

        try {
            await ApiClient.DeleteAsAsync<object>($"{_definitionRoute}/{id}");
            Snackbar.Add("Definition deleted", Severity.Success);
            await _dataGrid!.RefreshData();
        }
        catch (Exception ex) {
            Snackbar.Add($"Delete failed: {ex.Message}", Severity.Error);
        }
    }
}
