using Lyo.Web.Components;
using Lyo.Web.Primitives;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace Lyo.FileStorage.Web.Components.FileStorageManagement;

public partial class FileStoragePathTree
{
    /// <summary>Items bound to <see cref="MudTreeView{T}" />.</summary>
    [Parameter]
    [EditorRequired]
    public IReadOnlyCollection<TreeItemData<FileStoragePathTreeNode>> Items { get; set; } = [];

    /// <summary>Node currently selected.</summary>
    [Parameter]
    public FileStoragePathTreeNode? Selected { get; set; }

    /// <summary>Fired when the user clicks a node.</summary>
    [Parameter]
    public EventCallback<FileStoragePathTreeNode> SelectedChanged { get; set; }

    /// <summary>Fired when a directory is expanded.</summary>
    [Parameter]
    public EventCallback<FileStoragePathTreeNode> FolderExpanded { get; set; }

    /// <summary>Fired when the user collapses a directory so expansion state can be stored.</summary>
    [Parameter]
    public EventCallback<FileStoragePathTreeNode> FolderCollapsed { get; set; }

    /// <summary>Header refresh button.</summary>
    [Parameter]
    public EventCallback Refresh { get; set; }

    /// <summary>True while a QueryProject request is in flight.</summary>
    [Parameter]
    public bool Busy { get; set; }

    /// <summary>True when the listing stopped at the page cap.</summary>
    [Parameter]
    public bool Truncated { get; set; }

    /// <summary>Move, rotate, or delete using checked nodes (folders expand to descendant files).</summary>
    [Parameter]
    public EventCallback<IReadOnlyList<FileStoragePathTreeNode>> BulkMove { get; set; }

    /// <summary>Rotate DEKs on checked nodes.</summary>
    [Parameter]
    public EventCallback<IReadOnlyList<FileStoragePathTreeNode>> BulkRotateDeks { get; set; }

    /// <summary>Delete the checked nodes.</summary>
    [Parameter]
    public EventCallback<IReadOnlyList<FileStoragePathTreeNode>> BulkDelete { get; set; }

    /// <summary>Zip-download checked active files.</summary>
    [Parameter]
    public EventCallback<IReadOnlyList<FileStoragePathTreeNode>> BulkDownload { get; set; }

    /// <summary>Drop onto a directory.</summary>
    [Parameter]
    public EventCallback<FileStoragePathTreeDrop> Dropped { get; set; }

    /// <summary>Active file count for checked nodes (parent expands folders via QueryProject).</summary>
    [Parameter]
    public int BulkFileCount { get; set; }

    /// <summary>Fired when multi-select checkboxes change so the parent can resolve descendant file ids.</summary>
    [Parameter]
    public EventCallback<IReadOnlyList<FileStoragePathTreeNode>> CheckedChanged { get; set; }

    private readonly HashSet<FileStoragePathTreeNode> _checked = new(FileStoragePathTreeNodeKeyComparer.Instance);
    private FileStoragePathTreeNode? _dragNode;
    private string? _dropKey;
    private bool _editMode;
    private string _filter = string.Empty;

    private IReadOnlyCollection<TreeItemData<FileStoragePathTreeNode>> DisplayItems => FilterTree(Items, _filter);

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (!_editMode || _checked.Count == 0)
            return;

        var keys = _checked.Select(static n => n.Key).ToHashSet(StringComparer.Ordinal);
        _checked.Clear();
        RemapChecked(Items, keys, _checked);
    }

    private static string IconFor(FileStoragePathTreeNode node)
        => node.IsDirectory ? Icons.Material.Filled.Folder : Icons.Material.Filled.InsertDriveFile;

    private static Color IconColorFor(FileStoragePathTreeNode node)
        => node.IsDeleted ? Color.Warning : node.IsDirectory ? Color.Warning : Color.Default;

    private Task OnSelectAsync(FileStoragePathTreeNode node) => SelectedChanged.InvokeAsync(node);

    private Task OnExpandedAsync(FileStoragePathTreeNode node, bool expanded)
        => expanded ? FolderExpanded.InvokeAsync(node) : FolderCollapsed.InvokeAsync(node);

    private async Task ToggleEdit()
    {
        _editMode = !_editMode;
        if (_editMode)
            return;

        _checked.Clear();
        _dragNode = null;
        _dropKey = null;
        await CheckedChanged.InvokeAsync([]);
    }

    private Task OnCheckedChanged(IReadOnlyCollection<FileStoragePathTreeNode?> values)
    {
        _checked.Clear();
        foreach (var node in values) {
            if (node is { IsDeleted: false })
                _checked.Add(node);
        }

        return CheckedChanged.InvokeAsync(CheckedSnapshot());
    }

    private IReadOnlyList<FileStoragePathTreeNode> CheckedSnapshot() => _checked.ToList();

    private Task OnBulkMoveAsync() => BulkMove.InvokeAsync(CheckedSnapshot());

    private Task OnBulkRotateAsync() => BulkRotateDeks.InvokeAsync(CheckedSnapshot());

    private Task OnBulkDeleteAsync() => BulkDelete.InvokeAsync(CheckedSnapshot());

    private Task OnBulkDownloadAsync() => BulkDownload.InvokeAsync(CheckedSnapshot());

    private void OnDragStart(FileStoragePathTreeNode node)
    {
        _dragNode = node.IsDeleted ? null : node;
    }

    private void OnDragEnter(FileStoragePathTreeNode node)
    {
        if (_editMode && node.IsDirectory)
            _dropKey = node.Key;
    }

    private void OnDragLeave(FileStoragePathTreeNode node)
    {
        if (_dropKey == node.Key)
            _dropKey = null;
    }

    private Task OnDropAsync(FileStoragePathTreeNode target)
    {
        var dragged = _dragNode;
        _dragNode = null;
        _dropKey = null;
        if (!_editMode || dragged == null || dragged.IsDeleted || !target.IsDirectory)
            return Task.CompletedTask;

        var sources = _checked.Contains(dragged) ? CheckedSnapshot() : (IReadOnlyList<FileStoragePathTreeNode>) [dragged];
        sources = sources.Where(static s => !s.IsDeleted || s.IsDirectory).ToList();
        return Dropped.InvokeAsync(new FileStoragePathTreeDrop(sources, target));
    }

    private string ItemClass(FileStoragePathTreeNode node)
    {
        var css = _editMode && !node.IsDeleted ? "file-storage-tree-item-drag" : "file-storage-tree-item";
        if (_editMode && node.IsDirectory && _dropKey == node.Key)
            css += " file-storage-tree-drop-target";
        return css;
    }

    private static void RemapChecked(
        IEnumerable<ITreeItemData<FileStoragePathTreeNode>> items, HashSet<string> keys, HashSet<FileStoragePathTreeNode> into)
    {
        foreach (var item in items) {
            if (item.Value is { IsDeleted: false } && keys.Contains(item.Value.Key))
                into.Add(item.Value);
            if (item.Children is { Count: > 0 })
                RemapChecked(item.Children, keys, into);
        }
    }

    private static IReadOnlyCollection<TreeItemData<FileStoragePathTreeNode>> FilterTree(
        IReadOnlyCollection<TreeItemData<FileStoragePathTreeNode>> items, string filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
            return items;

        var needle = filter.Trim();
        var kept = new List<TreeItemData<FileStoragePathTreeNode>>();
        foreach (var item in items) {
            if (TryKeep(item, needle, out var filtered))
                kept.Add(filtered);
        }

        return kept;
    }

    private static bool TryKeep(ITreeItemData<FileStoragePathTreeNode> item, string needle, out TreeItemData<FileStoragePathTreeNode> filtered)
    {
        if (Matches(item.Value, needle)) {
            filtered = Clone(item, expand: item.Children is { Count: > 0 });
            return true;
        }

        var kept = new List<TreeItemData<FileStoragePathTreeNode>>();
        if (item.Children is { Count: > 0 }) {
            foreach (var child in item.Children) {
                if (TryKeep(child, needle, out var childFiltered))
                    kept.Add(childFiltered);
            }
        }

        if (kept.Count == 0) {
            filtered = null!;
            return false;
        }

        filtered = new TreeItemData<FileStoragePathTreeNode> {
            Value = item.Value,
            Text = item.Text,
            Icon = item.Icon,
            Expandable = true,
            Expanded = true,
            Children = kept
        };
        return true;
    }

    private static TreeItemData<FileStoragePathTreeNode> Clone(ITreeItemData<FileStoragePathTreeNode> item, bool expand)
    {
        List<TreeItemData<FileStoragePathTreeNode>>? children = null;
        if (item.Children is { Count: > 0 } source)
            children = source.Select(child => Clone(child, expand: child.Expanded)).ToList();

        return new TreeItemData<FileStoragePathTreeNode> {
            Value = item.Value,
            Text = item.Text,
            Icon = item.Icon,
            Expandable = children is { Count: > 0 },
            Expanded = expand,
            Children = children
        };
    }

    private static bool Matches(FileStoragePathTreeNode? node, string needle)
        => node != null
           && (node.Name.Contains(needle, StringComparison.OrdinalIgnoreCase)
               || (node.PathPrefix?.Contains(needle, StringComparison.OrdinalIgnoreCase) == true));
}
