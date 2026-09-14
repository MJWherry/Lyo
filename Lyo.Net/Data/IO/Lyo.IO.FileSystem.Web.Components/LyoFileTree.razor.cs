using Lyo.Exceptions;
using Lyo.IO.FileSystem;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lyo.IO.FileSystem.Web.Components;

/// <summary>Lazy folder tree over <see cref="IFileTreeSource" />.</summary>
public partial class LyoFileTree : ComponentBase
{
    private readonly HashSet<FileTreeNode> _checked = [];
    private bool _editMode;
    private string _filter = string.Empty;
    private List<TreeItemData<FileTreeNode>> _items = [];
    private FileTreeNode? _root;
    private FileTreeNode? _selected;

    /// <summary>Lists children. Required.</summary>
    [Parameter]
    [EditorRequired]
    public IFileTreeSource Source { get; set; } = default!;

    /// <summary>Root path passed to <see cref="IFileTreeSource.ListChildrenAsync" />.</summary>
    [Parameter]
    [EditorRequired]
    public string RootPath { get; set; } = "/";

    /// <summary>Caption for the root node.</summary>
    [Parameter]
    public string RootName { get; set; } = "/";

    /// <summary>Toolbar title.</summary>
    [Parameter]
    public string Title { get; set; } = "Files";

    /// <summary>Currently selected node.</summary>
    [Parameter]
    public FileTreeNode? Selected { get; set; }

    /// <summary>Fired when the selection changes.</summary>
    [Parameter]
    public EventCallback<FileTreeNode?> SelectedChanged { get; set; }

    /// <summary>True while a list request is in flight.</summary>
    [Parameter]
    public bool Busy { get; set; }

    /// <summary>True when the last list stopped at a cap.</summary>
    [Parameter]
    public bool Truncated { get; set; }

    /// <summary>Shows a search box that filters currently loaded nodes.</summary>
    [Parameter]
    public bool EnableFilter { get; set; } = true;

    /// <summary>Shows a multi-select toggle and checkboxes for bulk actions in the host.</summary>
    [Parameter]
    public bool EnableSelection { get; set; }

    /// <summary>Optional trailing content per row (presence chips, etc.). When unset, properties are not rendered.</summary>
    [Parameter]
    public RenderFragment<FileTreeNode>? ItemTrailing { get; set; }

    /// <summary>Shown under the search row while multi-select is on.</summary>
    [Parameter]
    public RenderFragment? SelectionToolbar { get; set; }

    /// <summary>Checked nodes when <see cref="EnableSelection" /> is on.</summary>
    [Parameter]
    public EventCallback<IReadOnlyList<FileTreeNode>> CheckedChanged { get; set; }

    /// <summary>True while the user is picking multiple rows.</summary>
    public bool EditMode => _editMode;

    /// <summary>Checked nodes in the current tree instance.</summary>
    public IReadOnlyList<FileTreeNode> CheckedNodes => _checked.ToList();

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        ArgumentHelpers.ThrowIfNull(Source);
        if (_root != null && _root.Entry.Path == RootPath)
            return;

        await ReloadAsync().ConfigureAwait(true);
    }

    /// <summary>Reloads the root listing.</summary>
    public Task RefreshAsync() => ReloadAsync();

    /// <summary>Loads every descendant under <paramref name="node" /> so a checked folder can expand to its files.</summary>
    public async Task LoadAllDescendantsAsync(FileTreeNode node)
    {
        ArgumentHelpers.ThrowIfNull(node);
        await LoadDescendantsCoreAsync(node).ConfigureAwait(true);
        RebuildItems();
        await InvokeAsync(StateHasChanged);
    }

    private async Task ReloadAsync()
    {
        var checkedPaths = _checked.Select(static n => n.Entry.Path).ToHashSet(StringComparer.Ordinal);
        _root = FileTreeLoader.CreateRoot(RootPath, RootName);
        await FileTreeLoader.EnsureChildrenAsync(Source, _root).ConfigureAwait(true);
        _selected = Selected;
        RebuildItems();
        if (checkedPaths.Count == 0 || _root == null)
            return;

        _checked.Clear();
        CollectMatching(_root, checkedPaths, _checked);
        await CheckedChanged.InvokeAsync(CheckedNodes).ConfigureAwait(true);
    }

    private async Task LoadDescendantsCoreAsync(FileTreeNode node)
    {
        if (!node.Entry.IsDirectory)
            return;

        await FileTreeLoader.EnsureChildrenAsync(Source, node).ConfigureAwait(true);
        foreach (var child in node.Children)
            await LoadDescendantsCoreAsync(child).ConfigureAwait(true);
    }

    private static void CollectMatching(FileTreeNode node, HashSet<string> paths, HashSet<FileTreeNode> into)
    {
        if (paths.Contains(node.Entry.Path))
            into.Add(node);

        foreach (var child in node.Children)
            CollectMatching(child, paths, into);
    }

    private async Task OnSelectedAsync(FileTreeNode? node)
    {
        _selected = node;
        await SelectedChanged.InvokeAsync(node).ConfigureAwait(true);
        if (node is { Entry.IsDirectory: true, ChildrenLoaded: false })
            await ExpandAsync(node).ConfigureAwait(true);
    }

    private async Task OnExpandedAsync(FileTreeNode? node, bool expanded)
    {
        if (!expanded || node == null || !node.Entry.IsDirectory)
            return;

        await ExpandAsync(node).ConfigureAwait(true);
    }

    private async Task ExpandAsync(FileTreeNode node)
    {
        await FileTreeLoader.EnsureChildrenAsync(Source, node).ConfigureAwait(true);
        RebuildItems();
        await InvokeAsync(StateHasChanged);
    }

    private async Task ToggleEditAsync()
    {
        _editMode = !_editMode;
        if (_editMode)
            return;

        _checked.Clear();
        await CheckedChanged.InvokeAsync([]).ConfigureAwait(true);
    }

    private Task OnRowCheckedChangedAsync(FileTreeNode node, bool isChecked)
    {
        if (isChecked)
            _checked.Add(node);
        else
            _checked.Remove(node);

        return CheckedChanged.InvokeAsync(CheckedNodes);
    }

    private IReadOnlyCollection<TreeItemData<FileTreeNode>> DisplayItems
        => FileTreeFilter.Apply(_items, _filter);

    internal static class FileTreeFilter
    {
        public static IReadOnlyCollection<TreeItemData<FileTreeNode>> Apply(
            IReadOnlyCollection<TreeItemData<FileTreeNode>> items, string filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
                return items;

            var needle = filter.Trim();
            var kept = new List<TreeItemData<FileTreeNode>>();
            foreach (var item in items) {
                if (TryKeep(item, needle, out var filtered))
                    kept.Add(filtered);
            }

            return kept;
        }

        private static bool TryKeep(TreeItemData<FileTreeNode> item, string needle, out TreeItemData<FileTreeNode> filtered)
        {
            if (Matches(item.Value, needle)) {
                filtered = item;
                return true;
            }

            var kept = new List<TreeItemData<FileTreeNode>>();
            if (item.Children is { Count: > 0 }) {
                foreach (var child in item.Children) {
                    if (child is TreeItemData<FileTreeNode> typed && TryKeep(typed, needle, out var childFiltered))
                        kept.Add(childFiltered);
                }
            }

            if (kept.Count == 0) {
                filtered = null!;
                return false;
            }

            filtered = new TreeItemData<FileTreeNode> {
                Value = item.Value,
                Text = item.Text,
                Icon = item.Icon,
                Expandable = true,
                Expanded = true,
                Children = kept
            };
            return true;
        }

        private static bool Matches(FileTreeNode? node, string needle)
            => node != null
               && (node.Entry.Name.Contains(needle, StringComparison.OrdinalIgnoreCase)
                   || node.Entry.Path.Contains(needle, StringComparison.OrdinalIgnoreCase));
    }

    private void RebuildItems()
    {
        if (_root == null) {
            _items = [];
            return;
        }

        _items = [ToItem(_root, expanded: true)];
    }

    private static TreeItemData<FileTreeNode> ToItem(FileTreeNode node, bool expanded)
    {
        List<TreeItemData<FileTreeNode>>? children = null;
        if (node.Entry.IsDirectory && !node.ChildrenLoaded)
            children = [];
        else if (node.Children.Count > 0)
            children = node.Children.Select(c => ToItem(c, expanded: false)).ToList();

        return new() {
            Value = node,
            Text = node.Entry.Name,
            Icon = node.Entry.IsDirectory ? Icons.Material.Filled.Folder : Icons.Material.Filled.InsertDriveFile,
            Expandable = node.Entry.IsDirectory,
            Expanded = expanded,
            Children = children
        };
    }
}
