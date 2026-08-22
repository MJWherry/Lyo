using Lyo.Api.Models.Common.Response;
using Lyo.Query.Models.Builders;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Common.Request;
using Lyo.Query.Models.Enums;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using SortDirection = Lyo.Common.Enums.SortDirection;

namespace Lyo.FileStorage.Web.Components.FileStorageManagement;

/// <summary>Tree tab: PathPrefix folder tree on the left, inspector on the right.</summary>
public partial class FileStorageTreeBrowser : ComponentBase, IDisposable
{
    private static readonly string[] SelectFields = ["Id", "PathPrefix", "OriginalFileName", "OriginalFileSize", "DeletedAt", "Availability"];

    private readonly HashSet<string> _expandedKeys = new(StringComparer.Ordinal) { FileStoragePathTreeBuilder.DirectoryKey(null) };
    private FileStorageBrowserActions? _actions;
    private bool _busy;
    private FileStoragePathTreeNode _root = FileStoragePathTreeBuilder.CreateRoot();
    private List<FileStoragePathTreeRow> _rows = [];
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
        await LoadRootAsync().ConfigureAwait(true);
        if (selectedKey != null)
            _selected = FileStoragePathTreeBuilder.Find(_root, selectedKey) ?? _root;

        await InvokeAsync(StateHasChanged);
    }

    private async Task LoadRootAsync()
    {
        _busy = true;
        try {
            (_rows, _truncated) = await QueryRowsAsync(where: null).ConfigureAwait(true);
            ApplyTree();
        }
        catch (Exception ex) {
            Host.SetStatus(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }
    }

    private void ApplyTree()
    {
        _root = FileStoragePathTreeBuilder.BuildFullTree(_rows, _truncated);
        if (_selected == null || FileStoragePathTreeBuilder.Find(_root, _selected.Key) == null)
            _selected = _root;

        RebuildTreeItems();
    }

    private Task OnFolderExpandedAsync(FileStoragePathTreeNode node)
    {
        if (node.IsDirectory)
            _expandedKeys.Add(node.Key);

        RebuildTreeItems();
        return Task.CompletedTask;
    }

    private void OnFolderCollapsed(FileStoragePathTreeNode node)
    {
        _expandedKeys.Remove(node.Key);
        RebuildTreeItems();
    }

    private async Task<(List<FileStoragePathTreeRow> Rows, bool Truncated)> QueryRowsAsync(WhereClause? where)
    {
        var rows = new List<FileStoragePathTreeRow>();
        var route = Host.FileMetadataQueryRoute.Trim().Trim('/') + "/QueryProject";
        for (var page = 0; page < FileStoragePathTreeBuilder.MaxPages; page++) {
            var builder = ProjectionQueryReqBuilder.New()
                .SetPagination(page * FileStoragePathTreeBuilder.PageSize, FileStoragePathTreeBuilder.PageSize)
                .AddSelects(SelectFields)
                .AddSort("OriginalFileName", SortDirection.Asc);
            var active = FileStorageGridRowHelper.CreateActiveFilesWhere();
            builder.AddWhere(where == null ? active : WhereClauseBuilder.CombineAs(GroupOperatorEnum.And, active, where));

            var result = await Host.ApiClient
                .PostAsAsync<ProjectionQueryReq, ProjectedQueryRes<object?>>(route, builder.Build())
                .ConfigureAwait(true);
            if (result is not { IsSuccess: true }) {
                throw new InvalidOperationException(result?.Error?.GetFullMessage() ?? "QueryProject failed.");
            }

            if (result.Items is not { Count: > 0 } items)
                return (rows, false);

            foreach (var item in items) {
                if (FileStoragePathTreeBuilder.TryReadRow(item, out var parsed) && !parsed.IsDeleted)
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

    private Task OnSelectedAsync(FileStoragePathTreeNode node)
    {
        _selected = node;
        ExpandTo(node);
        RebuildTreeItems();
        return InvokeAsync(StateHasChanged);
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

    private Task OnBulkMoveAsync(IReadOnlyList<FileStoragePathTreeNode> nodes)
        => _actions == null ? Task.CompletedTask : _actions.BulkMoveAsync(FileStoragePathTreeBuilder.CollectActiveFileIds(LiveNodes(nodes)));

    private Task OnBulkRotateAsync(IReadOnlyList<FileStoragePathTreeNode> nodes)
        => _actions == null ? Task.CompletedTask : _actions.RotateDeksAsync(FileStoragePathTreeBuilder.CollectActiveFileIds(LiveNodes(nodes)));

    private Task OnBulkDeleteAsync(IReadOnlyList<FileStoragePathTreeNode> nodes)
        => _actions == null ? Task.CompletedTask : _actions.DeleteFilesAsync(FileStoragePathTreeBuilder.CollectActiveFileIds(LiveNodes(nodes)));

    private Task OnBulkDownloadAsync(IReadOnlyList<FileStoragePathTreeNode> nodes)
        => _actions == null ? Task.CompletedTask : _actions.DownloadArchiveAsync(FileStoragePathTreeBuilder.CollectActiveFileIds(LiveNodes(nodes)));

    private async Task OnDroppedAsync(FileStoragePathTreeDrop drop)
    {
        if (_actions == null)
            return;

        var liveTarget = FileStoragePathTreeBuilder.Find(_root, drop.Target.Key);
        if (liveTarget is not { IsDirectory: true }) {
            Host.SetStatus("Drop target folder is no longer in the tree.", Severity.Warning);
            return;
        }

        var moves = FileStoragePathTreeBuilder.CollectMovesToDirectory(LiveNodes(drop.Sources), liveTarget);
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
        List<TreeItemData<FileStoragePathTreeNode>>? children = node.Children.Count > 0
            ? node.Children.Select(ToTreeItem).ToList()
            : null;
        return new() {
            Value = node,
            Text = node.Name,
            Icon = node.IsDirectory ? Icons.Material.Filled.Folder : Icons.Material.Filled.InsertDriveFile,
            Expandable = node.IsDirectory && node.Children.Count > 0,
            Expanded = node.IsDirectory && _expandedKeys.Contains(node.Key),
            Children = children
        };
    }
}
