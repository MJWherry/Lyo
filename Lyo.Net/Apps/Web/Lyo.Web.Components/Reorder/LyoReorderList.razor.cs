using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Components;

/// <summary>Ordered list with a drag handle, a read-only index, and up/down buttons. The list position is the order.</summary>
public partial class LyoReorderList<TItem>
{
    private int? _dragOverIndex;

    private int? _dragSourceIndex;

    /// <summary>Rows in display order. The component moves items inside this list.</summary>
    [Parameter]
    public List<TItem> Items { get; set; } = [];

    /// <summary>Body of one row.</summary>
    [Parameter]
    public RenderFragment<TItem> ItemContent { get; set; } = _ => builder => { };

    /// <summary>When set, a heading is shown before the first row of each consecutive group.</summary>
    [Parameter]
    public Func<TItem, string?>? SectionLabel { get; set; }

    /// <summary>Raised after a row is moved.</summary>
    [Parameter]
    public EventCallback Changed { get; set; }

    /// <summary>Raised when the delete button is clicked. Omit to hide the button.</summary>
    [Parameter]
    public EventCallback<TItem> OnDelete { get; set; }

    private bool ShowSection(int index)
    {
        if (SectionLabel == null || index < 0 || index >= Items.Count)
            return false;

        var label = SectionLabel(Items[index]);
        if (string.IsNullOrWhiteSpace(label))
            return false;

        if (index == 0)
            return true;

        return !string.Equals(label, SectionLabel(Items[index - 1]), StringComparison.Ordinal);
    }

    private string RowClass(int index)
    {
        if (_dragOverIndex == index && _dragSourceIndex is int from && from != index)
            return "lyo-reorder-drop";

        if (_dragSourceIndex == index)
            return "lyo-reorder-dragging";

        return "";
    }

    private void OnDragStart(DragEventArgs e, int index)
    {
        _dragSourceIndex = index;
        _dragOverIndex = null;
        if (e.DataTransfer != null)
            e.DataTransfer.EffectAllowed = "move";
    }

    private void OnDragEnter(int index)
    {
        if (_dragSourceIndex is int from && from != index)
            _dragOverIndex = index;
    }

    private void OnDragEnd()
    {
        _dragSourceIndex = null;
        _dragOverIndex = null;
    }

    private async Task OnDrop(int dropIndex)
    {
        if (_dragSourceIndex is not int from)
            return;

        OnDragEnd();
        var insertAt = dropIndex > from ? dropIndex - 1 : dropIndex;
        await Move(from, insertAt);
    }

    private async Task Move(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= Items.Count || toIndex < 0 || toIndex >= Items.Count || fromIndex == toIndex)
            return;

        var item = Items[fromIndex];
        Items.RemoveAt(fromIndex);
        Items.Insert(toIndex, item);
        if (Changed.HasDelegate)
            await Changed.InvokeAsync();
    }
}