using Lyo.Reporting.Models;
using Lyo.Reporting.Models.Composition;
using Lyo.Reporting.Models.Controls;
using Lyo.Reporting.Models.Models;
using Microsoft.AspNetCore.Components;

namespace Lyo.Reporting.Web.Components;

/// <summary>Live composition editor: same paint as <see cref="ReportViewer{T}" />, plus hover chrome, handle-only drag, and selection. Not used for generate HTML/PDF.</summary>
public partial class ReportDesignCanvas
{
    private enum ChromeAdd
    {
        None,
        Section,
        Grid
    }

    private static readonly ContentType[] AddBlockTypes =
        Enum.GetValues<ContentType>().OrderBy(t => t.ToString(), StringComparer.OrdinalIgnoreCase).ToArray();

    [Parameter]
    [EditorRequired]
    public Report<object> Report { get; set; } = null!;

    /// <summary>Example parameter map. Interpolates <c>{Key}</c> at paint time without cloning <see cref="Report" />.</summary>
    [Parameter]
    public IReadOnlyDictionary<string, string?>? PreviewParameters { get; set; }

    [Parameter]
    public ReportDesignSelection? Selection { get; set; }

    [Parameter]
    public EventCallback<ReportDesignHit> OnHit { get; set; }

    [Parameter]
    public EventCallback<ReportDesignDropRequest> OnDrop { get; set; }

    [Parameter]
    public EventCallback<ReportDesignHit> OnDelete { get; set; }

    [Parameter]
    public EventCallback<ReportDesignAddRequest> OnAdd { get; set; }

    [Parameter]
    public EventCallback<ReportDesignCardDrop> OnCardDrop { get; set; }

    [Parameter]
    public EventCallback<ReportDesignGridColumnDrop> OnGridColumnDrop { get; set; }

    [Parameter]
    public string ChartScriptUrl { get; set; } = Constants.Charts.DefaultScriptUrl;

    private Layout Layout => Report.Layout ?? new();

    private ReportViewPainter Paint => new(Layout, Report.Styles, PreviewParameters, Report.Sections);

    private SectionBodyItem? _dragging;
    private bool _dropHandled;
    private string? _hoverKey;
    private string? _dropSlot;

    private string? RootComponentCaption => string.IsNullOrWhiteSpace(Layout.RootComponentType)
        ? null
        : $"HTML/PDF generate uses root component {Layout.RootComponentType}. This canvas still edits the stored composition.";

    private bool ReportChromeSelected
        => Selection is { Section: null, Control: null, Card: null, Block: null, Table: null, Grid: null, TableColumn: null, TableRow: null };

    private string TitleStyle
    {
        get
        {
            var ring = ReportChromeSelected
                ? Paint.SelectionRing(true) + " cursor: pointer;"
                : "cursor: pointer;";
            return "margin-bottom: 30px; padding-bottom: 20px; border-bottom: 3px solid " + Paint.AccentColor + "; " + ring;
        }
    }

    private bool IsChromeActive(string key, bool selected)
        => selected || string.Equals(_hoverKey, key, StringComparison.Ordinal);

    private bool IsHover(string key)
        => string.Equals(_hoverKey, key, StringComparison.Ordinal);

    private string ChromeBox(bool selected, bool hover)
    {
        if (selected)
            return $" outline: 2px solid {Paint.AccentColor}; outline-offset: 2px;";
        if (hover)
            return $" outline: 1px dashed {Paint.AccentColor}; outline-offset: 2px;";
        return string.Empty;
    }

    private string HiddenDim(string? visibleWhen)
    {
        if (PreviewParameters is null || ReportCompositionProcessor.IsVisible(visibleWhen, PreviewParameters))
            return string.Empty;

        return " opacity: 0.45;";
    }

    private static string CollapsedDim(Section section)
        => section.Collapsed ? " opacity: 0.45;" : string.Empty;

    private string SlotStyle(bool over, bool dragging)
    {
        var height = over ? "28px" : dragging ? "20px" : "12px";
        var bg = over ? Paint.AccentColor + "33" : dragging ? "#e2e8f0" : "transparent";
        return $"min-height: {height}; margin: 2px 0; border-radius: 4px; background: {bg};";
    }

    private static string SlotKey(Section section, int index)
        => "slot:" + ReportDesignTreeExpand.LiveKey(section) + ":" + index;

    private void SetHover(string key) => _hoverKey = key;

    private void ClearHover(string key)
    {
        if (string.Equals(_hoverKey, key, StringComparison.Ordinal))
            _hoverKey = null;
    }

    private void ClearDropSlot(string key)
    {
        if (string.Equals(_dropSlot, key, StringComparison.Ordinal))
            _dropSlot = null;
    }

    private bool ItemSelected(SectionBodyItem item)
        => item.Kind switch {
            SectionBodyKind.Control when item.Control is not null => ControlOrChildSelected(item.Control),
            SectionBodyKind.Subsection when item.Subsection is not null => Selection?.Matches(item.Subsection) == true,
            _ => false
        };

    private bool ControlOrChildSelected(Control control)
    {
        if (Selection?.Matches(control) == true)
            return true;

        if (control is Table table)
            return Selection?.TableColumn is not null && table.Columns.Contains(Selection.TableColumn)
                || Selection?.TableRow is not null && table.Rows.Contains(Selection.TableRow);

        if (control is not Grid grid)
            return false;

        foreach (var child in grid.Controls) {
            if (ControlOrChildSelected(child))
                return true;
        }

        return false;
    }

    private static ReportDesignHit ItemDeleteHit(Section section, SectionBodyItem item)
        => item.Kind switch {
            SectionBodyKind.Control when item.Control is Card card => new() { Section = section, Control = card, Card = card },
            SectionBodyKind.Control when item.Control is Block block => new() { Section = section, Control = block, Block = block },
            SectionBodyKind.Control when item.Control is Table table => new() { Section = section, Control = table, Table = table },
            SectionBodyKind.Control when item.Control is Grid grid => new() { Section = section, Control = grid, Grid = grid },
            SectionBodyKind.Subsection => new() { Section = item.Subsection },
            _ => new() { Section = section }
        };

    private static ReportDesignHit ItemHit(Section section, SectionBodyItem item)
        => item.Kind switch {
            SectionBodyKind.Control when item.Control is Card card => new() { Section = section, Control = card, Card = card },
            SectionBodyKind.Control when item.Control is Block block => new() { Section = section, Control = block, Block = block },
            SectionBodyKind.Control when item.Control is Table table => new() { Section = section, Control = table, Table = table },
            SectionBodyKind.Control when item.Control is Grid grid => new() { Section = section, Control = grid, Grid = grid },
            SectionBodyKind.Subsection => new() { Section = item.Subsection },
            _ => new() { Section = section }
        };

    private Task SelectReportChrome()
    {
        if (_dragging is not null && !_dropHandled)
            ClearDrag();

        return OnHit.InvokeAsync(new());
    }

    private Task Hit(ReportDesignHit hit)
    {
        if (_dragging is not null && !_dropHandled)
            ClearDrag();

        return OnHit.InvokeAsync(hit);
    }

    private Task HitBody(Section section, SectionBodyItem item)
    {
        if (_dragging is not null && !_dropHandled)
            ClearDrag();

        return OnHit.InvokeAsync(ItemHit(section, item));
    }

    private void OnMoveDragStart(SectionBodyItem item)
    {
        _dragging = item;
        _dropHandled = false;
        StateHasChanged();
    }

    private async Task OnMoveDragEnd()
    {
        if (_dropHandled) {
            ClearDrag();
            return;
        }

        await Task.Delay(80);
        await InvokeAsync(() => {
            if (!_dropHandled)
                ClearDrag();
        });
    }

    private void ClearDrag()
    {
        _dragging = null;
        _dropSlot = null;
    }

    private Task DeleteHit(ReportDesignHit hit) => OnDelete.InvokeAsync(hit);

    private Task Add(ReportDesignAddRequest request) => OnAdd.InvokeAsync(request);

    private async Task DropOn(Section dest, int insertIndex)
    {
        _dropHandled = true;
        _dropSlot = null;
        if (_dragging is null)
            return;

        var drag = _dragging;
        _dragging = null;
        await OnDrop.InvokeAsync(new() {
            Source = drag.Section,
            Dest = dest,
            Item = drag,
            InsertIndex = insertIndex
        });
    }

    private async Task DropOnGrid(Section dest, Grid grid)
    {
        _dropHandled = true;
        _dropSlot = null;
        if (_dragging?.Control is null or Grid)
            return;

        var drag = _dragging;
        _dragging = null;
        await OnDrop.InvokeAsync(new() {
            Source = drag.Section,
            Dest = dest,
            Item = drag,
            InsertIndex = grid.Controls.Count,
            DestGrid = grid
        });
    }
}
