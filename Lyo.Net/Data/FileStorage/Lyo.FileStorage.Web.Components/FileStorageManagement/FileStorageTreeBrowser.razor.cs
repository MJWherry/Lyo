using Lyo.Api.Models.Common.Response;
using Lyo.Query.Models.Builders;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Common.Request;
using Lyo.Query.Models.Enums;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using SortDirection = Lyo.Common.Core.Enums.SortDirection;

namespace Lyo.FileStorage.Web.Components.FileStorageManagement;

/// <summary>Tree tab: PathPrefix folder tree on the left, inspector on the right. Each folder is queried when opened, not on first render.</summary>
public partial class FileStorageTreeBrowser : ComponentBase, IDisposable
{
    private static readonly string[] SelectFields = ["Id", "PathPrefix", "OriginalFileName", "OriginalFileSize", "SourceFileName", "DeletedAt", "Availability"];
    private static readonly string[] IdSelectFields = ["Id", "PathPrefix", "DeletedAt", "Availability"];

    private readonly HashSet<string> _expandedKeys = new(StringComparer.Ordinal) { FileStoragePathTreeBuilder.DirectoryKey(null) };
    private FileStorageBrowserActions? _actions;
    private int _bulkFileCount;
    private bool _busy;
    private FileStoragePathTreeNode _root = FileStoragePathTreeBuilder.CreateRoot();
    private FileStoragePathTreeNode? _selected;
    private IReadOnlyCollection<TreeItemData<FileStoragePathTreeNode>> _treeItems = [];
    private bool _truncated;

    /// <summary>Cascaded parent (API client, dialogs).</summary>
    [CascadingParameter]
    public FileStorageManagement Host { get; set; } = default!;

    /// <inheritdoc />
    public void Dispose() => Host.FilesChanged -= RefreshAsync;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        _actions = new FileStorageBrowserActions(Host, RefreshAsync);
        Host.FilesChanged += RefreshAsync;
        _selected = _root;
        await LoadRootAsync().ConfigureAwait(true);
    }

    private async Task RefreshAsync()
    {
        var selectedKey = _selected?.Key;
        var expanded = _expandedKeys.ToList();
        await LoadRootAsync().ConfigureAwait(true);
        foreach (var key in expanded.OrderBy(static k => k.Count(static c => c == '/'))) {
            var node = FileStoragePathTreeBuilder.Find(_root, key);
            if (node is { IsDirectory: true })
                await LoadFolderAsync(node).ConfigureAwait(true);
        }

        if (selectedKey != null)
            _selected = FileStoragePathTreeBuilder.Find(_root, selectedKey) ?? _root;

        if (_selected is { IsDirectory: true, ChildrenLoaded: false })
            await LoadFolderAsync(_selected).ConfigureAwait(true);

        RebuildTreeItems();
        await InvokeAsync(StateHasChanged);
    }

    private async Task LoadRootAsync()
    {
        _busy = true;
        try {
            var selectedKey = _selected?.Key;
            _root = FileStoragePathTreeBuilder.CreateRoot();
            await LoadFolderAsync(_root).ConfigureAwait(true);
            _selected = selectedKey == null
                ? _root
                : FileStoragePathTreeBuilder.Find(_root, selectedKey) ?? _root;

            RebuildTreeItems();
        }
        catch (Exception ex) {
            Host.SetStatus(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }
    }

    private async Task LoadFolderAsync(FileStoragePathTreeNode dir)
    {
        if (!dir.IsDirectory || dir.ChildrenLoaded)
            return;

        // Root uses the same DeletedAt/Availability filter as the Files grid (no PathPrefix SQL). Nested folders add StartsWith and split files vs child
        // folders in memory so PathPrefix null/empty OR-groups cannot hide rows the grid already shows.
        var extra = dir.PathPrefix == null
            ? null
            : WhereClauseBuilder.Condition("PathPrefix", ComparisonOperatorEnum.StartsWith, FileStoragePathTreeBuilder.Normalize(dir.PathPrefix)!);
        var (rows, truncated) = await QueryRowsAsync(extra, SelectFields, includeDeleted: true).ConfigureAwait(true);
        var listed = await ListStorageKeysAsync(dir.PathPrefix).ConfigureAwait(true);
        rows = FileStorageStorageKeyJoin.ApplyPresence(rows, listed);
        var prefixes = rows.ConvertAll(static r => r.PathPrefix);
        FileStoragePathTreeBuilder.MergeImmediateChildren(dir, rows, prefixes, truncated);
        if (listed != null)
            FileStoragePathTreeBuilder.AddCloudOnlyOrphans(dir, FileStorageStorageKeyJoin.UnmatchedKeys(listed, rows));
        _truncated = _root.Truncated || (_selected?.IsDirectory == true && _selected.Truncated);
    }

    private async Task<HashSet<string>?> ListStorageKeysAsync(string? prefix)
    {
        try {
            var path = "diagnostics/storage-keys?maxKeys=10000";
            if (!string.IsNullOrWhiteSpace(prefix))
                path += "&prefix=" + Uri.EscapeDataString(prefix);

            var keys = await Host.ApiClient.GetAsAsync<List<string>>(Host.FilesApi(path)).ConfigureAwait(true);
            return (keys ?? []).ToHashSet(StringComparer.Ordinal);
        }
        catch {
            return null;
        }
    }

    private async Task<(List<FileStoragePathTreeRow> Rows, bool Truncated)> QueryRowsAsync(WhereClause? where, IReadOnlyList<string> select, bool includeDeleted = false)
    {
        var rows = new List<FileStoragePathTreeRow>();
        var route = Host.FileMetadataQueryRoute.Trim().Trim('/') + "/QueryProject";
        for (var page = 0; page < FileStoragePathTreeBuilder.MaxPages; page++) {
            var builder = ProjectionQueryReqBuilder.New()
                .SetPagination(page * FileStoragePathTreeBuilder.PageSize, FileStoragePathTreeBuilder.PageSize)
                .AddSelects(select.ToArray())
                .AddSort("OriginalFileName", SortDirection.Asc);
            if (includeDeleted) {
                if (where != null)
                    builder.AddWhere(where);
            }
            else {
                builder.AddWhere(FileStorageGridRowHelper.CombineWithActive(where));
            }

            var result = await Host.ApiClient
                .PostAsAsync<ProjectionQueryReq, ProjectedQueryRes<object?>>(route, builder.Build())
                .ConfigureAwait(true);
            if (result is not { IsSuccess: true }) {
                throw new InvalidOperationException(result?.Error?.GetFullMessage() ?? "QueryProject failed.");
            }

            if (result.Items is not { Count: > 0 } items)
                return (rows, false);

            foreach (var item in items) {
                if (FileStoragePathTreeBuilder.TryReadRow(item, out var parsed) && (includeDeleted || !parsed.IsDeleted))
                    rows.Add(parsed);
            }

            var pageFull = items.Count >= FileStoragePathTreeBuilder.PageSize;
            if (result.HasMore == false || !pageFull)
                return (rows, false);

            if (page == FileStoragePathTreeBuilder.MaxPages - 1)
                return (rows, true);
        }

        return (rows, false);
    }

    private async Task<IReadOnlyList<Guid>> ResolveActiveFileIdsAsync(IReadOnlyList<FileStoragePathTreeNode> nodes)
    {
        var ids = new HashSet<Guid>();
        foreach (var node in LiveNodes(nodes)) {
            if (!node.IsDirectory) {
                if (node is { FileId: { } id, IsDeleted: false })
                    ids.Add(id);
                continue;
            }

            var (rows, _) = await QueryRowsAsync(FileStoragePathTreeBuilder.CreateSubtreeWhere(node.PathPrefix), IdSelectFields)
                .ConfigureAwait(true);
            foreach (var row in rows) {
                if (!row.IsDeleted)
                    ids.Add(row.FileId);
            }
        }

        return [.. ids];
    }

    private async Task OnSelectedAsync(FileStoragePathTreeNode node)
    {
        var live = FileStoragePathTreeBuilder.Find(_root, node.Key) ?? node;
        _selected = live;
        ExpandTo(live);
        RebuildTreeItems();
        await InvokeAsync(StateHasChanged);
        if (!live.IsDirectory || live.ChildrenLoaded) {
            _truncated = _root.Truncated || (_selected.IsDirectory && _selected.Truncated);
            return;
        }

        _busy = true;
        try {
            await LoadFolderAsync(live).ConfigureAwait(true);
        }
        catch (Exception ex) {
            Host.SetStatus(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }

        _truncated = _root.Truncated || _selected.Truncated;
        RebuildTreeItems();
        await InvokeAsync(StateHasChanged);
    }

    private async Task OnFolderExpandedAsync(FileStoragePathTreeNode node)
    {
        var live = FileStoragePathTreeBuilder.Find(_root, node.Key) ?? node;
        if (!live.IsDirectory)
            return;

        _expandedKeys.Add(live.Key);
        _busy = true;
        try {
            await LoadFolderAsync(live).ConfigureAwait(true);
        }
        catch (Exception ex) {
            Host.SetStatus(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }

        RebuildTreeItems();
        await InvokeAsync(StateHasChanged);
    }

    private void OnFolderCollapsed(FileStoragePathTreeNode node)
    {
        _expandedKeys.Remove(node.Key);
        RebuildTreeItems();
    }

    private void ExpandTo(FileStoragePathTreeNode node)
    {
        _expandedKeys.Add(FileStoragePathTreeBuilder.DirectoryKey(null));
        string? acc = null;
        foreach (var segment in FileStoragePathTreeBuilder.SplitSegments(node.PathPrefix)) {
            acc = acc == null ? segment : $"{acc}/{segment}";
            _expandedKeys.Add(FileStoragePathTreeBuilder.DirectoryKey(acc));
        }
    }

    private void RebuildTreeItems()
        => _treeItems = new List<TreeItemData<FileStoragePathTreeNode>> { ToTreeItem(_root) };

    private IReadOnlyList<FileStoragePathTreeNode> LiveNodes(IEnumerable<FileStoragePathTreeNode> nodes)
    {
        List<FileStoragePathTreeNode> live = [];
        foreach (var node in nodes) {
            var found = FileStoragePathTreeBuilder.Find(_root, node.Key);
            if (found != null)
                live.Add(found);
        }

        return live;
    }

    private async Task OnCheckedAsync(IReadOnlyList<FileStoragePathTreeNode> nodes)
    {
        try {
            var ids = await ResolveActiveFileIdsAsync(nodes).ConfigureAwait(true);
            _bulkFileCount = ids.Count;
        }
        catch (Exception ex) {
            _bulkFileCount = FileStoragePathTreeBuilder.CollectActiveFileIds(LiveNodes(nodes)).Count;
            Host.SetStatus(ex.Message, Severity.Error);
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task OnBulkMoveAsync(IReadOnlyList<FileStoragePathTreeNode> nodes)
    {
        if (_actions == null)
            return;

        await _actions.BulkMoveAsync(await ResolveActiveFileIdsAsync(nodes).ConfigureAwait(true)).ConfigureAwait(true);
    }

    private async Task OnBulkRotateAsync(IReadOnlyList<FileStoragePathTreeNode> nodes)
    {
        if (_actions == null)
            return;

        await _actions.RotateDeksAsync(await ResolveActiveFileIdsAsync(nodes).ConfigureAwait(true)).ConfigureAwait(true);
    }

    private async Task OnBulkDeleteAsync(IReadOnlyList<FileStoragePathTreeNode> nodes)
    {
        if (_actions == null)
            return;

        await _actions.DeleteFilesAsync(await ResolveActiveFileIdsAsync(nodes).ConfigureAwait(true)).ConfigureAwait(true);
    }

    private async Task OnBulkDownloadAsync(IReadOnlyList<FileStoragePathTreeNode> nodes)
    {
        if (_actions == null)
            return;

        await _actions.DownloadArchiveAsync(await ResolveActiveFileIdsAsync(nodes).ConfigureAwait(true)).ConfigureAwait(true);
    }

    private async Task OnDroppedAsync(FileStoragePathTreeDrop drop)
    {
        if (_actions == null)
            return;

        var liveTarget = FileStoragePathTreeBuilder.Find(_root, drop.Target.Key);
        if (liveTarget is not { IsDirectory: true }) {
            Host.SetStatus("Drop target folder is no longer in the tree.", Severity.Warning);
            return;
        }

        List<FileStoragePathTreeNode> sources = [];
        foreach (var src in LiveNodes(drop.Sources)) {
            if (!src.IsDirectory) {
                sources.Add(src);
                continue;
            }

            var (rows, _) = await QueryRowsAsync(FileStoragePathTreeBuilder.CreateSubtreeWhere(src.PathPrefix), SelectFields)
                .ConfigureAwait(true);
            var subtree = FileStoragePathTreeBuilder.BuildFullTree(rows, truncated: false);
            sources.Add(FileStoragePathTreeBuilder.FindDirectory(subtree, src.PathPrefix) ?? src);
        }

        var moves = FileStoragePathTreeBuilder.CollectMovesToDirectory(sources, liveTarget);
        if (moves.Count == 0) {
            Host.SetStatus("Cannot move into that folder (same path, or a folder into itself).", Severity.Warning);
            return;
        }

        var dest = drop.Target.PathPrefix ?? FileStoragePathTreeBuilder.RootDisplayName;
        var confirm = await Host.DialogService.ShowMessageBoxAsync(
            "Move files", $"Move {moves.Count} file(s) into {drop.Target.Name} ({dest})?", "Move", cancelText: "Cancel");
        if (confirm != true)
            return;

        await _actions.MoveFilesToPrefixesAsync(moves).ConfigureAwait(true);
    }

    private TreeItemData<FileStoragePathTreeNode> ToTreeItem(FileStoragePathTreeNode node)
    {
        List<TreeItemData<FileStoragePathTreeNode>>? children;
        if (node.IsDirectory && !node.ChildrenLoaded) {
            children = [
                new TreeItemData<FileStoragePathTreeNode> {
                    Value = FileStoragePathTreeBuilder.CreatePendingChild(node),
                    Text = "…",
                    Expandable = false
                }
            ];
        }
        else if (node.Children.Count > 0)
            children = node.Children.Select(ToTreeItem).ToList();
        else
            children = null;

        return new() {
            Value = node,
            Text = node.Name,
            Icon = node.IsDirectory ? Icons.Material.Filled.Folder : Icons.Material.Filled.InsertDriveFile,
            Expandable = node.IsDirectory && (!node.ChildrenLoaded || node.Children.Count > 0),
            Expanded = node.IsDirectory && _expandedKeys.Contains(node.Key),
            Children = children
        };
    }
}
