using Lyo.Common.Metadata.Records;
using Lyo.Query.Models.Builders;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Enums;
using Lyo.Web.Components.Models;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Lyo.Config.Web.Components;

public partial class ConfigDefinitionGrid
{
    /// <summary>Store used to create, edit, and delete definitions (writes never go through generic CRUD).</summary>
    [Parameter]
    [EditorRequired]
    public IConfigStore Store { get; set; } = null!;

    /// <summary>
    /// Feeds the grid's search, filter, sort, and export through Lyo.Query (<see cref="ConfigQueryRoutes.Definition" />). Writes still go through <see cref="Store" />.
    /// </summary>
    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    /// <summary>Entity type listed in the grid. Empty skips the load.</summary>
    [Parameter]
    public string SubjectEntityType { get; set; } = "";

    /// <summary>Bumped by the shell when the operator applies a new scope or asks for a refresh.</summary>
    [Parameter]
    public int DataVersion { get; set; }

    /// <summary>Fired after a successful create, edit, or delete so sibling tabs can reload.</summary>
    [Parameter]
    public EventCallback OnChanged { get; set; }

    /// <summary>Fired when the operator opens definition history for a key.</summary>
    [Parameter]
    public EventCallback<Guid> OnViewDefinitionHistory { get; set; }

    private static readonly LyoDataGridFeatureFlags _gridFeatures =
        LyoDataGridFeatureFlags.Filterable | LyoDataGridFeatureFlags.Searchable | LyoDataGridFeatureFlags.BulkMenu | LyoDataGridFeatureFlags.BulkExport;

    private readonly List<FilterPropertyDefinition> _propertyDefinitions = [
        new("Key"),
        new("ForValueType", "Value type"),
        new("IsRequired", "Required", LyoTypeInfo.Bool),
        new("IsEncrypted", "Encrypted", LyoTypeInfo.Bool)
    ];

    private LyoDataGridProjected? _dataGrid;
    private string? _queryType;
    private int _queryVersion = int.MinValue;

    protected override async Task OnParametersSetAsync()
    {
        if (string.Equals(_queryType, SubjectEntityType, StringComparison.Ordinal) && _queryVersion == DataVersion)
            return;

        _queryType = SubjectEntityType;
        _queryVersion = DataVersion;
        if (_dataGrid != null && !string.IsNullOrWhiteSpace(SubjectEntityType))
            await _dataGrid.RefreshData();
    }

    private void ApplyQuery(ProjectionQueryReqBuilder query)
    {
        if (!string.IsNullOrWhiteSpace(SubjectEntityType))
            query.AddWhere(new ConditionClause("SubjectEntityType", ComparisonOperatorEnum.Equals, SubjectEntityType.Trim()));
    }

    private async Task OpenCreateAsync()
    {
        var parameters = new DialogParameters<ConfigDefinitionView> {
            { d => d.Store, Store },
            { d => d.SubjectEntityType, SubjectEntityType.Trim() }
        };
        var dialog = await DialogService.ShowAsync<ConfigDefinitionView>("New definition", parameters, LyoDialogPresets.Medium);
        var result = await dialog.Result;
        if (result is { Canceled: false })
            await AfterDefinitionSavedAsync(result.Data);
    }

    private async Task OpenEditAsync(ConfigDefinitionRecord definition)
    {
        var parameters = new DialogParameters<ConfigDefinitionView> {
            { d => d.Store, Store },
            { d => d.Definition, definition },
            { d => d.SubjectEntityType, SubjectEntityType.Trim() }
        };
        var dialog = await DialogService.ShowAsync<ConfigDefinitionView>("Edit definition", parameters, LyoDialogPresets.Medium);
        var result = await dialog.Result;
        if (result is { Canceled: false })
            await AfterDefinitionSavedAsync(result.Data);
    }

    private async Task OpenEditProjectedAsync(object? item)
    {
        if (!TryGetId(item, out var id))
            return;

        var definition = await Store.GetDefinitionByIdAsync(id);
        if (definition == null) {
            Snackbar.Add("Definition was not found.", Severity.Warning);
            return;
        }

        await OpenEditAsync(definition);
    }

    private async Task DeleteProjectedAsync(object? item)
    {
        if (!TryGetId(item, out var id))
            return;

        var definition = await Store.GetDefinitionByIdAsync(id);
        if (definition == null) {
            Snackbar.Add("Definition was not found.", Severity.Warning);
            return;
        }

        await DeleteAsync(definition);
    }

    private Task ViewHistoryProjectedAsync(object? item)
        => TryGetId(item, out var id) ? ViewHistoryAsync(id) : Task.CompletedTask;

    private async Task BulkDeleteAsync()
    {
        var selected = ProjectedGridKeys.RowsFromKeys(_dataGrid?.SelectedKeys);
        if (selected.Count == 0)
            return;

        var ids = selected.Select(i => ProjectedValueHelper.GetValue(i, "Id"))
            .Select(v => ProjectedValueHelper.TryGetGuid(v, out var g) ? g : (Guid?)null)
            .Where(g => g.HasValue)
            .Select(g => g!.Value)
            .ToList();
        if (ids.Count == 0)
            return;

        if (!await DialogService.ConfirmDeleteAsync($"{ids.Count} definition(s)", "Bindings and revision history for those keys are removed (cascade).", "Delete definitions"))
            return;

        var deleted = 0;
        foreach (var id in ids) {
            try {
                await Store.DeleteDefinitionAsync(id);
                deleted++;
            }
            catch (Exception ex) {
                Snackbar.Add(ex.Message, Severity.Error);
            }
        }

        if (deleted > 0) {
            Snackbar.Add($"Deleted {deleted} definition(s).", Severity.Success);
            if (_dataGrid is not null)
                await _dataGrid.ClearSelectionAsync();
            await NotifyChangedAsync();
        }
    }

    private async Task DeleteAsync(ConfigDefinitionRecord definition)
    {
        if (!await DialogService.ConfirmDeleteAsync($"'{definition.Key}'", "Bindings and revision history for this key are removed (cascade).", "Delete definition"))
            return;

        try {
            await Store.DeleteDefinitionAsync(definition.Id);
            Snackbar.Add($"Deleted '{definition.Key}'.", Severity.Success);
            await NotifyChangedAsync();
        }
        catch (Exception ex) {
            Snackbar.Add(ex.Message, Severity.Error);
        }
    }

    private async Task AfterDefinitionSavedAsync(object? data)
    {
        await NotifyChangedAsync();
        if (data is Guid id && id != default)
            await OnViewDefinitionHistory.InvokeAsync(id);
    }

    private Task ViewHistoryAsync(Guid definitionId)
        => OnViewDefinitionHistory.InvokeAsync(definitionId);

    private static bool TryGetId(object? item, out Guid id)
    {
        var raw = ProjectedValueHelper.GetValue(item, "Id");
        return ProjectedValueHelper.TryGetGuid(raw, out id);
    }

    private async Task NotifyChangedAsync()
    {
        if (_dataGrid != null)
            await _dataGrid.RefreshData();

        await OnChanged.InvokeAsync();
    }
}
