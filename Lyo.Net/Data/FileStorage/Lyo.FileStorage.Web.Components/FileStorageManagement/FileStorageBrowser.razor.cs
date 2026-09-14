using Lyo.Web.Components.DataGrid;
using Microsoft.AspNetCore.Components;

namespace Lyo.FileStorage.Web.Components.FileStorageManagement;

/// <summary>Pair of QueryProject grids over file metadata: operator columns, plus expected storage keys derived from those rows.</summary>
public partial class FileStorageBrowser : ComponentBase, IDisposable
{
    private static readonly string[] FileKeySelectFields = ["Id", "SourceFileName", "PathPrefix", "DeletedAt", "Availability"];

    private FileStorageBrowserActions? _actions;
    private HashSet<string> _existingKeys = new(StringComparer.Ordinal);
    private LyoDataGridProjected? _metadataGrid;
    private LyoDataGridProjected? _storageGrid;

    /// <summary>Cascaded parent (API client, dialogs).</summary>
    [CascadingParameter]
    public FileStorageManagement Host { get; set; } = default!;

    private bool DiagnosticsAvailable { get; set; }

    private LyoDataGridFeatureFlags GridFeatures
        => LyoDataGridFeatureFlags.Filterable | LyoDataGridFeatureFlags.Searchable | LyoDataGridFeatureFlags.AutoRefresh | LyoDataGridFeatureFlags.BulkMenu;

    /// <inheritdoc />
    public void Dispose() => Host.FilesChanged -= RefreshAsync;

    protected override async Task OnInitializedAsync()
    {
        _actions = new FileStorageBrowserActions(Host, RefreshAsync);
        Host.FilesChanged += RefreshAsync;
        await LoadStorageKeysAsync();
    }

    private async Task RefreshAsync()
    {
        await LoadStorageKeysAsync();
        if (_metadataGrid != null) {
            await _metadataGrid.ClearSelectionAsync();
            await _metadataGrid.RefreshData();
        }

        if (_storageGrid != null) {
            await _storageGrid.ClearSelectionAsync();
            await _storageGrid.RefreshData();
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task LoadStorageKeysAsync()
    {
        try {
            var keys = await Host.ApiClient.GetAsAsync<List<string>>(Host.FilesApi("diagnostics/storage-keys?maxKeys=10000"));
            _existingKeys = (keys ?? []).ToHashSet(StringComparer.Ordinal);
            DiagnosticsAvailable = _existingKeys.Count > 0 || keys != null;
        }
        catch {
            DiagnosticsAvailable = false;
            _existingKeys = [];
        }
    }

    private bool KeyExists(string? expected)
        => FileStorageGridRowHelper.StorageKeyExists(_existingKeys, expected);

    private IReadOnlyList<object?> SelectedRows(LyoDataGridProjected? grid)
        => ProjectedGridKeys.RowsFromKeys(grid?.SelectedKeys);
}
