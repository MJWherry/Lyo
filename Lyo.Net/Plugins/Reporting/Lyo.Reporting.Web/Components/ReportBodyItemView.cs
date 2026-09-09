using Lyo.Reporting.Models.Composition;
using Lyo.Reporting.Models.Controls;
using Lyo.Reporting.Models.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Lyo.Reporting.Web.Components;

/// <summary>Paints one mixed body item. No drag chrome — the design canvas wraps this component when editing.</summary>
public sealed class ReportBodyItemView : ComponentBase
{
    [Parameter]
    [EditorRequired]
    public ReportViewPainter Painter { get; set; } = null!;

    [Parameter]
    [EditorRequired]
    public SectionBodyItem Item { get; set; } = null!;

    [Parameter]
    public ReportDesignSelection? Selection { get; set; }

    [Parameter]
    public EventCallback<ReportDesignHit> OnHit { get; set; }

    [Parameter]
    public EventCallback<ReportDesignCardDrop> OnCardDrop { get; set; }

    [Parameter]
    public EventCallback<ReportDesignGridColumnDrop> OnGridColumnDrop { get; set; }

    private Control? _cardDrag;
    private TableColumn? _tableColDrag;
    private string? _cardSlot;
    private string? _tableColSlot;
    private bool _dropHandled;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (OnHit.HasDelegate)
            WriteInteractive(builder);
        else
            Painter.WriteBodyItem(builder, Item);
    }

    private void WriteInteractive(RenderTreeBuilder b)
    {
        switch (Item.Control) {
            case Card card:
                WriteCard(b, Item.Section, null, card);
                break;
            case Block block:
                Painter.WriteContentBlock(b, block);
                break;
            case Table table:
                WriteTable(b, table);
                break;
            case Grid grid:
                WriteLayoutGrid(b, grid);
                break;
        }
    }

    private void WriteLayoutGrid(RenderTreeBuilder b, Grid grid)
    {
        var section = Item.Section;
        if (!string.IsNullOrEmpty(grid.Title)) {
            b.OpenElement(2, "h3");
            b.AddAttribute(3, "style", $"color: {Painter.InkColor}; font-size: 18px; font-weight: 600; margin-bottom: 10px;");
            b.AddContent(4, Painter.P(grid.Title));
            b.CloseElement();
        }

        b.OpenElement(0, "div");
        b.AddAttribute(1, "style", Painter.GetGridWrapperStyles(grid));
        if (OnCardDrop.HasDelegate) {
            b.AddAttribute(2, "ondragover", EventCallback.Factory.Create(this, () => SetCardSlot(grid, grid.Controls.Count)));
            b.AddEventPreventDefaultAttribute(3, "ondragover", true);
            b.AddAttribute(4, "ondrop", EventCallback.Factory.Create(this, () => DropCard(grid, grid.Controls.Count)));
            b.AddEventPreventDefaultAttribute(5, "ondrop", true);
        }

        var index = 0;
        foreach (var child in grid.Controls) {
            var captured = child;
            var insertAt = index;
            b.OpenElement(10, "div");
            var span = ChildSpan(captured);
            var ring = Painter.SelectionRing(Selection?.Matches(captured) == true);
            var highlight = IsCardSlot(grid, insertAt) ? "outline: 2px dashed #2563eb; outline-offset: 2px;" : string.Empty;
            var style = string.Join("; ", new[] { span, ring, highlight, "cursor: pointer; position: relative;" }.Where(s => !string.IsNullOrEmpty(s)));
            if (!string.IsNullOrEmpty(style))
                b.AddAttribute(11, "style", style);

            b.AddAttribute(12, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, () => HitControl(section, captured)));
            b.AddEventStopPropagationAttribute(13, "onclick", true);
            if (OnCardDrop.HasDelegate) {
                b.AddAttribute(14, "ondragover", EventCallback.Factory.Create(this, () => SetCardSlot(grid, insertAt)));
                b.AddEventPreventDefaultAttribute(15, "ondragover", true);
                b.AddEventStopPropagationAttribute(16, "ondragover", true);
                b.AddAttribute(17, "ondrop", EventCallback.Factory.Create(this, () => DropCard(grid, insertAt)));
                b.AddEventPreventDefaultAttribute(18, "ondrop", true);
                b.AddEventStopPropagationAttribute(19, "ondrop", true);
            }

            WriteCardHandle(b, grid, captured);
            Painter.WriteControl(b, captured);
            b.CloseElement();
            index++;
        }

        b.CloseElement();
    }

    private static string ChildSpan(Control control)
    {
        var parts = new List<string> { "min-width: 0" };
        if (control.ColumnSpan > 1)
            parts.Add($"grid-column: span {control.ColumnSpan}");

        if (control.RowSpan > 1)
            parts.Add($"grid-row: span {control.RowSpan}");

        return string.Join("; ", parts);
    }

    private void WriteCard(RenderTreeBuilder b, Section section, Grid? grid, Card card)
    {
        var extra = Painter.SelectionRing(Selection?.Matches(card) == true) + " cursor: pointer; position: relative;";
        Painter.WriteCard(b, card, extra, inner => {
            inner.AddAttribute(16, "data-lyo-design", ReportDesignTreeExpand.CardKey(section, card));
            inner.AddAttribute(17, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, () => OnHit.InvokeAsync(new() { Section = section, Control = card, Card = card })));
            inner.AddEventStopPropagationAttribute(18, "onclick", true);
            if (grid is not null)
                WriteCardHandle(inner, grid, card);
        });
    }

    private void WriteCardHandle(RenderTreeBuilder b, Grid grid, Control control)
    {
        if (!OnCardDrop.HasDelegate)
            return;

        b.OpenElement(20, "span");
        b.AddAttribute(21, "draggable", "true");
        b.AddAttribute(22, "title", "Move");
        b.AddAttribute(23, "style", "position: absolute; top: 4px; right: 4px; cursor: grab; color: #64748b; font-size: 14px; line-height: 1;");
        b.AddAttribute(24, "ondragstart", "if(event.dataTransfer){event.dataTransfer.setData('text/plain','lyo-card');event.dataTransfer.effectAllowed='move';}");
        b.AddAttribute(25, "onmousedown", EventCallback.Factory.Create<MouseEventArgs>(this, () => OnCardDragStart(control)));
        b.AddAttribute(26, "ondragend", EventCallback.Factory.Create(this, OnCardDragEnd));
        b.AddEventStopPropagationAttribute(27, "onclick", true);
        b.AddContent(28, "⋮⋮");
        b.CloseElement();
    }

    private void WriteTable(RenderTreeBuilder b, Table table)
    {
        var section = Item.Section;
        Painter.WriteTable(b, table, OnGridColumnDrop.HasDelegate
            ? (inner, column, index) => WriteTableHeader(inner, section, table, column, index)
            : null);
    }

    private void WriteTableHeader(RenderTreeBuilder b, Section section, Table table, TableColumn column, int index)
    {
        WriteTableColSlot(b, table, index, 40);
        b.OpenElement(50, "span");
        b.AddAttribute(51, "style", "display: inline-flex; align-items: center; gap: 4px; cursor: pointer;");
        b.AddAttribute(52, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, () => OnHit.InvokeAsync(new() { Section = section, Table = table, TableColumn = column })));
        b.AddEventStopPropagationAttribute(53, "onclick", true);
        if (OnGridColumnDrop.HasDelegate) {
            b.OpenElement(54, "span");
            b.AddAttribute(55, "draggable", "true");
            b.AddAttribute(56, "title", "Move column");
            b.AddAttribute(57, "style", "cursor: grab; color: #64748b;");
            b.AddAttribute(58, "ondragstart", "if(event.dataTransfer){event.dataTransfer.setData('text/plain','lyo-gcol');event.dataTransfer.effectAllowed='move';}");
            b.AddAttribute(59, "onmousedown", EventCallback.Factory.Create<MouseEventArgs>(this, () => OnTableColDragStart(column)));
            b.AddAttribute(60, "ondragend", EventCallback.Factory.Create(this, OnTableColDragEnd));
            b.AddEventStopPropagationAttribute(61, "onclick", true);
            b.AddContent(62, "⋮⋮");
            b.CloseElement();
        }

        b.AddContent(63, Painter.P(column.Header));
        b.CloseElement();
        if (index == table.Columns.Count - 1)
            WriteTableColSlot(b, table, index + 1, 80);
    }

    private void WriteTableColSlot(RenderTreeBuilder b, Table table, int insertIndex, int seq)
    {
        var slotKey = "gcol:" + ReportDesignTreeExpand.LiveKey(table) + ":" + insertIndex;
        var over = string.Equals(_tableColSlot, slotKey, StringComparison.Ordinal);
        var dragging = _tableColDrag is not null;
        b.OpenElement(seq, "span");
        b.AddAttribute(seq + 1, "style", GridSlotStyle(over, dragging));
        b.AddAttribute(seq + 2, "ondragover", EventCallback.Factory.Create(this, () => _tableColSlot = slotKey));
        b.AddEventPreventDefaultAttribute(seq + 3, "ondragover", true);
        b.AddAttribute(seq + 4, "ondragleave", EventCallback.Factory.Create(this, () => ClearTableColSlot(slotKey)));
        b.AddAttribute(seq + 5, "ondrop", EventCallback.Factory.Create(this, () => DropTableColumn(table, insertIndex)));
        b.AddEventPreventDefaultAttribute(seq + 6, "ondrop", true);
        b.CloseElement();
    }

    private static string GridSlotStyle(bool over, bool dragging)
    {
        var width = over ? "16px" : dragging ? "10px" : "4px";
        var bg = over ? "#2563eb33" : dragging ? "#e2e8f0" : "transparent";
        return $"display: inline-block; width: {width}; min-height: 16px; vertical-align: middle; border-radius: 2px; background: {bg};";
    }

    private Task HitControl(Section section, Control control)
        => OnHit.InvokeAsync(control switch {
            Card card => new ReportDesignHit { Section = section, Control = card, Card = card },
            Block block => new ReportDesignHit { Section = section, Control = block, Block = block },
            Table table => new ReportDesignHit { Section = section, Control = table, Table = table },
            Grid grid => new ReportDesignHit { Section = section, Control = grid, Grid = grid },
            _ => new ReportDesignHit { Section = section, Control = control }
        });

    private void OnCardDragStart(Control control)
    {
        _cardDrag = control;
        _dropHandled = false;
        StateHasChanged();
    }

    private async Task OnCardDragEnd()
    {
        if (_dropHandled) {
            ClearCardDrag();
            return;
        }

        await Task.Delay(80);
        await InvokeAsync(() => {
            if (!_dropHandled)
                ClearCardDrag();
        });
    }

    private void ClearCardDrag()
    {
        _cardDrag = null;
        _cardSlot = null;
    }

    private static string CardSlotKey(Grid grid, int insertIndex)
        => "card:" + ReportDesignTreeExpand.LiveKey(grid) + ":" + insertIndex;

    private bool IsCardSlot(Grid grid, int insertIndex)
        => string.Equals(_cardSlot, CardSlotKey(grid, insertIndex), StringComparison.Ordinal);

    private void SetCardSlot(Grid grid, int insertIndex)
    {
        var key = CardSlotKey(grid, insertIndex);
        if (string.Equals(_cardSlot, key, StringComparison.Ordinal))
            return;

        _cardSlot = key;
        StateHasChanged();
    }

    private async Task DropCard(Grid grid, int insertIndex)
    {
        _dropHandled = true;
        _cardSlot = null;
        if (_cardDrag is null)
            return;

        var control = _cardDrag;
        _cardDrag = null;
        await OnCardDrop.InvokeAsync(new() { Grid = grid, Control = control, InsertIndex = insertIndex });
    }

    private void OnTableColDragStart(TableColumn column)
    {
        _tableColDrag = column;
        _dropHandled = false;
        StateHasChanged();
    }

    private async Task OnTableColDragEnd()
    {
        if (_dropHandled) {
            ClearTableColDrag();
            return;
        }

        await Task.Delay(80);
        await InvokeAsync(() => {
            if (!_dropHandled)
                ClearTableColDrag();
        });
    }

    private void ClearTableColDrag()
    {
        _tableColDrag = null;
        _tableColSlot = null;
    }

    private void ClearTableColSlot(string key)
    {
        if (string.Equals(_tableColSlot, key, StringComparison.Ordinal))
            _tableColSlot = null;
    }

    private async Task DropTableColumn(Table table, int insertIndex)
    {
        _dropHandled = true;
        _tableColSlot = null;
        if (_tableColDrag is null)
            return;

        var column = _tableColDrag;
        _tableColDrag = null;
        await OnGridColumnDrop.InvokeAsync(new() { Table = table, Column = column, InsertIndex = insertIndex });
    }
}
