using Lyo.Web.Components.DataGrid;
using Microsoft.AspNetCore.Components;

namespace Lyo.FileStorage.Web.Components.FileStorageWorkbench;

/// <summary>Two QueryProject grids over file metadata: operator columns, and expected storage keys derived from those rows.</summary>
public partial class FileStorageBrowser : ComponentBase, IDisposable
{
    private static readonly string[] FileKeySelectFields = ["Id", "SourceFileName", "PathPrefix", "DeletedAt", "Availability"];

    private FileStorageBrowserActions? _actions;
    private HashSet<string> _existingKeys = new(StringComparer.Ordinal);
    private LyoDataGridProjected? _metadataGrid;
    private LyoDataGridProjected? _storageGrid;

    /// <summary>Cascaded workbench host (API client, dialogs).</summary>
    [CascadingParameter]
    public FileStorageWorkbench Workbench { get; set; } = default!;

    private bool DiagnosticsAvailable { get; set; }

    private LyoDataGridFeatureFlags GridFeatures
        => LyoDataGridFeatureFlags.Filterable | LyoDataGridFeatureFlags.Searchable | LyoDataGridFeatureFlags.AutoRefresh | LyoDataGridFeatureFlags.BulkMenu;

    /// <inheritdoc />
    public void Dispose() => Workbench.FilesChanged -= RefreshAsync;

    protected override async Task OnInitializedAsync()
    {
        _actions = new FileStorageBrowserActions(Workbench, RefreshAsync);
        Workbench.FilesChanged += RefreshAsync;
        await LoadStorageKeysAsync();
    }

    private async Task RefreshAsync()
    {
        await LoadStorageKeysAsync();
        if (_metadataGrid != null)
            await _metadataGrid.RefreshData();

        if (_storageGrid != null)
            await _storageGrid.RefreshData();

        await InvokeAsync(StateHasChanged);
    }

    private async Task LoadStorageKeysAsync()
    {
        try {
            var keys = await Workbench.ApiClient.GetAsAsync<List<string>>(Workbench.FilesApi("diagnostics/storage-keys?maxKeys=10000"));
            _existingKeys = (keys ?? []).ToHashSet(StringComparer.Ordinal);
            DiagnosticsAvailable = _existingKeys.Count > 0 || keys != null;
        }
        catch {
            DiagnosticsAvailable = false;
            _existingKeys = [];
        }
    }

    private bool KeyExists(string? expected)
    {
        if (string.IsNullOrWhiteSpace(expected) || _existingKeys.Count == 0)
            return false;

        if (_existingKeys.Contains(expected))
            return true;

        foreach (var key in _existingKeys) {
            if (key.Length > expected.Length && key.EndsWith(expected, StringComparison.Ordinal) && key[key.Length - expected.Length - 1] == '/')
                return true;
        }

        return false;
    }

    private IReadOnlyList<object?> SelectedRows(LyoDataGridProjected? grid)
        => grid?.SelectedItems is { Count: > 0 } selected ? selected.ToList() : [];
}
