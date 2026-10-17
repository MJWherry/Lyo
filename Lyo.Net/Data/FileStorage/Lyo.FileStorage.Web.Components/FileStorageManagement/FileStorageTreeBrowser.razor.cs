using Lyo.Api.FileStorage.Models;
using Lyo.FileMetadataStore.Models;
using Lyo.IO.FileSystem;
using Lyo.IO.FileSystem.Web.Components;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lyo.FileStorage.Web.Components.FileStorageManagement;

/// <summary>Tree tab: folder-list API on the left, inspector on the right. The Files grid stays QueryProject.</summary>
public partial class FileStorageTreeBrowser : ComponentBase, IDisposable
{
    private FileStorageBrowserActions? _actions;
    private FileStorageHttpTreeSource? _folderSource;
    private FileStoragePathTreeNode? _selected;
    private LyoFileTree? _tree;
    private IReadOnlyList<FileTreeNode> _checked = [];
    private int _bulkCatalogCount;
    private int _bulkPhysicalCount;
    private bool _hasCheckedFolder;
    private bool _bulkBusy;
    private bool _truncated;
    private int _listingRevision;
    private readonly List<StructureRow> _structure = [];
    private List<FileStorageTagTypeResponse> _tagTypes = [];
    private List<FileStorageTagValueKindResponse> _tagValueKinds = [];
    private string? _addType;
    private string _addKind = "string";
    private string _addGrain = "Value";

    /// <summary>Cascaded parent (API client, dialogs).</summary>
    [CascadingParameter]
    public FileStorageManagement Host { get; set; } = default!;

    /// <inheritdoc />
    public void Dispose() => Host.FilesChanged -= RefreshAsync;

    private bool BlobStructure => _folderSource?.IsBlobStructure != false;

    private FileStorageTagTypeResponse? SelectedType => _tagTypes.FirstOrDefault(type => type.TagType == _addType);

    private string EffectiveKind => SelectedType is { Automatic: true } type ? type.ValueKind : _addKind;

    private IReadOnlyList<FileStorageTagValueKindResponse> KindChoices
        => _tagValueKinds.Count > 0 ? _tagValueKinds : [new(EffectiveKind, SelectedType?.Sort ?? "Ordinal", SelectedType?.Grains ?? ["Value"])];

    private bool SelectedTypeNeedsGrain => SelectedGrains.Any(grain => !string.Equals(grain, "Value", StringComparison.OrdinalIgnoreCase));

    private IReadOnlyList<string> SelectedGrains
    {
        get
        {
            foreach (var kind in _tagValueKinds) {
                if (string.Equals(kind.Kind, EffectiveKind, StringComparison.OrdinalIgnoreCase))
                    return kind.Grains;
            }

            return SelectedType?.Grains ?? [];
        }
    }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        _actions = new FileStorageBrowserActions(Host, RefreshAsync);
        Host.FilesChanged += RefreshAsync;
        _folderSource = new FileStorageHttpTreeSource(Host.ApiClient, Host.FilesApi);
        _selected = FileStorageHttpTreeSource.DirectoryNode(null);
        try {
            var capabilities = await Host.ApiClient.GetAsAsync<FileStorageCapabilitiesResponse>(Host.FilesApi("capabilities")).ConfigureAwait(true);
            _tagTypes = capabilities?.TagTypes?.ToList() ?? [];
            _tagValueKinds = capabilities?.TagValueKinds?.ToList() ?? [];
        }
        catch (Exception ex) {
            Host.SetStatus($"Tag types were not loaded: {ex.Message}", Severity.Warning);
        }
    }

    private async Task UseBlobAsync()
    {
        _structure.Clear();
        _folderSource?.UseBlobStructure();
        _selected = FileStorageHttpTreeSource.DirectoryNode(null);
        await RefreshAsync().ConfigureAwait(true);
    }

    private void TypeChanged(string? type)
    {
        _addType = type;
        _addKind = SelectedType?.ValueKind ?? "string";
        _addGrain = FirstGrain(EffectiveKind);
    }

    private void KindChanged(string kind)
    {
        if (SelectedType is { Automatic: true })
            return;

        _addKind = kind;
        _addGrain = FirstGrain(kind);
    }

    private async Task AddStructureAsync()
    {
        if (string.IsNullOrWhiteSpace(_addType) || !FileTagValueKinds.TryParse(EffectiveKind, out var kind))
            return;

        var grainName = SelectedTypeNeedsGrain ? _addGrain : "Value";
        if (!Enum.TryParse<FileTagGrain>(grainName, true, out var grain))
            return;

        var key = new FileTagGroupKey(_addType, kind, grain);
        _structure.Add(new(key.Format(), key.Format().Replace(':', ' ')));
        ApplyStructure();
        _selected = FileStorageHttpTreeSource.DirectoryNode(null);
        await RefreshAsync().ConfigureAwait(true);
    }

    private string FirstGrain(string kindName)
    {
        if (!FileTagValueKinds.TryParse(kindName, out var kind))
            return "Value";

        foreach (var grain in FileTagValueKinds.GrainsFor(kind)) {
            if (grain != FileTagGrain.Value)
                return grain.ToString();
        }

        return "Value";
    }

    private async Task RemoveStructureAsync(StructureRow row)
    {
        _structure.Remove(row);
        ApplyStructure();
        _selected = FileStorageHttpTreeSource.DirectoryNode(null);
        await RefreshAsync().ConfigureAwait(true);
    }

    private void ApplyStructure()
    {
        if (_folderSource == null)
            return;

        if (_structure.Count == 0)
            _folderSource.UseBlobStructure();
        else
            _folderSource.UseTagStructure(_structure.Select(static row => row.Key).ToArray());
    }

    private IFileTreeSource FolderSource => _folderSource ??= new FileStorageHttpTreeSource(Host.ApiClient, Host.FilesApi);

    private FileStorageBackendKind BackendKind => _folderSource?.BackendKind ?? FileStorageBackendKind.None;

    private async Task RefreshAsync()
    {
        _listingRevision++;
        if (_tree != null)
            await _tree.RefreshAsync().ConfigureAwait(true);

        _truncated = _folderSource?.Truncated == true;
        await InvokeAsync(StateHasChanged);
    }

    private async Task OnTreeSelectedAsync(FileTreeNode? node)
    {
        if (node == null) {
            _selected = FileStorageHttpTreeSource.DirectoryNode(null);
            return;
        }

        _selected = FileStorageHttpTreeSource.ToPathNode(node.Entry);
        _truncated = _folderSource?.Truncated == true;
        await InvokeAsync(StateHasChanged);
    }

    private Task OnInspectorSelectedAsync(FileStoragePathTreeNode? node)
    {
        _selected = node ?? FileStorageHttpTreeSource.DirectoryNode(null);
        return Task.CompletedTask;
    }

    private async Task OnTreeChangedAsync()
    {
        if (_folderSource != null && _selected != null)
            CollectPending(_selected);

        await RefreshAsync().ConfigureAwait(true);
    }

    private bool BulkObjectActionsEnabled => !_bulkBusy && (_bulkCatalogCount > 0 || _bulkPhysicalCount > 0 || _hasCheckedFolder);

    private bool BulkCatalogActionsEnabled => !_bulkBusy && (_bulkCatalogCount > 0 || _hasCheckedFolder);

    private void OnCheckedChanged(IReadOnlyList<FileTreeNode> nodes)
    {
        _checked = nodes;
        CountLoaded(nodes, out _bulkCatalogCount, out _bulkPhysicalCount);
        _hasCheckedFolder = nodes.Any(static n => n.Entry.IsDirectory);
    }

    private async Task OnBulkMoveAsync()
    {
        var (ids, keys) = await ResolveCheckedAsync().ConfigureAwait(true);
        if (_actions == null)
            return;

        if (ids.Count > 0)
            await _actions.BulkMoveAsync(ids).ConfigureAwait(true);

        if (keys.Count > 0)
            await _actions.MovePhysicalAsync(keys).ConfigureAwait(true);
    }

    private async Task OnBulkCopyAsync()
    {
        var (ids, keys) = await ResolveCheckedAsync().ConfigureAwait(true);
        if (_actions == null)
            return;

        if (ids.Count > 0)
            await _actions.BulkCopyAsync(ids).ConfigureAwait(true);

        if (keys.Count > 0)
            await _actions.CopyPhysicalAsync(keys).ConfigureAwait(true);
    }

    private async Task OnBulkRotateAsync()
    {
        var (ids, keys) = await ResolveCheckedAsync().ConfigureAwait(true);
        if (keys.Count > 0)
            Host.SetStatus($"{keys.Count} backend-only object(s) skipped (DEK rotate needs the catalog).", Severity.Warning);

        if (_actions != null)
            await _actions.RotateDeksAsync(ids).ConfigureAwait(true);
    }

    private async Task OnBulkDownloadAsync()
    {
        var (ids, keys) = await ResolveCheckedAsync().ConfigureAwait(true);
        if (_actions == null)
            return;

        if (ids.Count > 0)
            await _actions.DownloadArchiveAsync(ids).ConfigureAwait(true);
        else if (keys.Count > 0)
            Host.SetStatus("Zip is catalog-only; downloading backend objects one at a time.", Severity.Info);

        if (keys.Count > 0)
            await _actions.DownloadPhysicalManyAsync(keys).ConfigureAwait(true);
    }

    private async Task OnBulkDeleteAsync()
    {
        var (ids, keys) = await ResolveCheckedAsync().ConfigureAwait(true);
        if (_actions == null)
            return;

        if (ids.Count > 0)
            await _actions.DeleteFilesAsync(ids).ConfigureAwait(true);

        if (keys.Count > 0)
            await _actions.DeletePhysicalAsync(keys).ConfigureAwait(true);
    }

    private async Task<(IReadOnlyList<Guid> CatalogIds, IReadOnlyList<string> PhysicalKeys)> ResolveCheckedAsync()
    {
        _bulkBusy = true;
        try {
            var ids = new HashSet<Guid>();
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var node in _checked) {
                if (_tree != null && node.Entry.IsDirectory)
                    await _tree.LoadAllDescendantsAsync(node).ConfigureAwait(true);

                FileStoragePresenceUi.Collect(node, ids, keys);
            }

            _bulkCatalogCount = ids.Count;
            _bulkPhysicalCount = keys.Count;
            return (ids.ToList(), keys.ToList());
        }
        finally {
            _bulkBusy = false;
        }
    }

    private static void CountLoaded(IEnumerable<FileTreeNode> nodes, out int catalog, out int physical)
    {
        var ids = new HashSet<Guid>();
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var node in nodes)
            FileStoragePresenceUi.Collect(node, ids, keys);

        catalog = ids.Count;
        physical = keys.Count;
    }

    private void CollectPending(FileStoragePathTreeNode node)
    {
        if (node is { IsPending: true, IsDirectory: true, PathPrefix: { } prefix })
            _folderSource!.PendingPrefixes.Add(prefix);

        foreach (var child in node.Children)
            CollectPending(child);
    }

    private sealed class StructureRow(string key, string label)
    {
        public string Label { get; } = label;

        public string Key { get; } = key;
    }
}
