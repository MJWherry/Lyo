using Lyo.Api.FileStorage.Models;
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
    private bool _hasCheckedFolder;
    private bool _bulkBusy;
    private bool _truncated;
    private int _listingRevision;

    /// <summary>Cascaded parent (API client, dialogs).</summary>
    [CascadingParameter]
    public FileStorageManagement Host { get; set; } = default!;

    /// <inheritdoc />
    public void Dispose() => Host.FilesChanged -= RefreshAsync;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _actions = new FileStorageBrowserActions(Host, RefreshAsync);
        Host.FilesChanged += RefreshAsync;
        _folderSource = new FileStorageHttpTreeSource(Host.ApiClient, Host.FilesApi);
        _selected = FileStorageHttpTreeSource.DirectoryNode(null);
    }

    private IFileTreeSource FolderSource => _folderSource ??= new FileStorageHttpTreeSource(Host.ApiClient, Host.FilesApi);

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

    private bool BulkActionsEnabled => !_bulkBusy && (_bulkCatalogCount > 0 || _hasCheckedFolder);

    private void OnCheckedChanged(IReadOnlyList<FileTreeNode> nodes)
    {
        _checked = nodes;
        _bulkCatalogCount = CountLoadedCatalog(nodes);
        _hasCheckedFolder = nodes.Any(static n => n.Entry.IsDirectory);
    }

    private async Task OnBulkMoveAsync()
    {
        var ids = await ResolveCheckedCatalogIdsAsync().ConfigureAwait(true);
        if (_actions != null)
            await _actions.BulkMoveAsync(ids).ConfigureAwait(true);
    }

    private async Task OnBulkRotateAsync()
    {
        var ids = await ResolveCheckedCatalogIdsAsync().ConfigureAwait(true);
        if (_actions != null)
            await _actions.RotateDeksAsync(ids).ConfigureAwait(true);
    }

    private async Task OnBulkDownloadAsync()
    {
        var ids = await ResolveCheckedCatalogIdsAsync().ConfigureAwait(true);
        if (_actions != null)
            await _actions.DownloadArchiveAsync(ids).ConfigureAwait(true);
    }

    private async Task OnBulkDeleteAsync()
    {
        var ids = await ResolveCheckedCatalogIdsAsync().ConfigureAwait(true);
        if (_actions != null)
            await _actions.DeleteFilesAsync(ids).ConfigureAwait(true);
    }

    private async Task<IReadOnlyList<Guid>> ResolveCheckedCatalogIdsAsync()
    {
        _bulkBusy = true;
        try {
            var ids = new HashSet<Guid>();
            var skippedPhysical = 0;
            foreach (var node in _checked) {
                if (_tree != null && node.Entry.IsDirectory)
                    await _tree.LoadAllDescendantsAsync(node).ConfigureAwait(true);

                skippedPhysical += CollectCatalogIds(node, ids);
            }

            _bulkCatalogCount = ids.Count;
            if (skippedPhysical > 0)
                Host.SetStatus($"{skippedPhysical} physical-only object(s) skipped (not in the catalog).", Severity.Warning);

            return ids.ToList();
        }
        finally {
            _bulkBusy = false;
        }
    }

    private static int CollectCatalogIds(FileTreeNode node, HashSet<Guid> ids)
    {
        if (node.Entry.IsDirectory) {
            var skipped = 0;
            foreach (var child in node.Children)
                skipped += CollectCatalogIds(child, ids);

            return skipped;
        }

        var presence = FileStoragePresenceUi.Read(node.Entry);
        Guid? fileId = FileStoragePresenceUi.TryFileId(node.Entry, out var parsed) ? parsed : null;
        if (FileStoragePresenceUi.InCatalog(presence, fileId) && fileId is { } catalogId) {
            ids.Add(catalogId);
            return 0;
        }

        return presence == FileStoragePresence.Physical ? 1 : 0;
    }

    private static int CountLoadedCatalog(IEnumerable<FileTreeNode> nodes)
    {
        var ids = new HashSet<Guid>();
        foreach (var node in nodes)
            CollectCatalogIds(node, ids);

        return ids.Count;
    }

    private void CollectPending(FileStoragePathTreeNode node)
    {
        if (node is { IsPending: true, IsDirectory: true, PathPrefix: { } prefix })
            _folderSource!.PendingPrefixes.Add(prefix);

        foreach (var child in node.Children)
            CollectPending(child);
    }
}
