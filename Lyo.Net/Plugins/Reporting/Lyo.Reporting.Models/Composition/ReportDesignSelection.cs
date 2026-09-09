using System.Runtime.CompilerServices;
using Lyo.Exceptions;
using Lyo.Reporting.Models.Controls;
using Lyo.Reporting.Models.Models;

namespace Lyo.Reporting.Models.Composition;

/// <summary>Workbench / canvas selection. Compared by object identity so live preview and the tree stay on the same instance.</summary>
public sealed class ReportDesignSelection
{
    public Section? Section { get; init; }

    public Control? Control { get; init; }

    public Card? Card { get; init; }

    public Block? Block { get; init; }

    public Table? Table { get; init; }

    public Grid? Grid { get; init; }

    public TableColumn? TableColumn { get; init; }

    public TableRow? TableRow { get; init; }

    /// <summary>True when <paramref name="candidate" /> is the selected object.</summary>
    public bool Matches(object? candidate)
    {
        if (candidate is null)
            return false;

        return ReferenceEquals(candidate, Card)
            || ReferenceEquals(candidate, Block)
            || ReferenceEquals(candidate, Table)
            || ReferenceEquals(candidate, Grid)
            || ReferenceEquals(candidate, TableColumn)
            || ReferenceEquals(candidate, TableRow)
            || ReferenceEquals(candidate, Control)
            || (Card is null && Block is null && Table is null && Grid is null && TableColumn is null && TableRow is null && Control is null && ReferenceEquals(candidate, Section));
    }
}

/// <summary>A click on the design canvas.</summary>
public sealed class ReportDesignHit
{
    public Section? Section { get; init; }

    public Control? Control { get; init; }

    public Card? Card { get; init; }

    public Block? Block { get; init; }

    public Table? Table { get; init; }

    public Grid? Grid { get; init; }

    public TableColumn? TableColumn { get; init; }

    public TableRow? TableRow { get; init; }
}

/// <summary>Reorder a control within a grid.</summary>
public sealed class ReportDesignCardDrop
{
    public required Grid Grid { get; init; }

    public required Control Control { get; init; }

    public int InsertIndex { get; init; }
}

/// <summary>Reorder a table column (and lockstep cells) on the canvas.</summary>
public sealed class ReportDesignGridColumnDrop
{
    public required Table Table { get; init; }

    public required TableColumn Column { get; init; }

    public int InsertIndex { get; init; }
}

/// <summary>A canvas drop: relocate <see cref="Item" /> into <see cref="Dest" /> at <see cref="InsertIndex" />.</summary>
public sealed class ReportDesignDropRequest
{
    public required Section Source { get; init; }

    public required Section Dest { get; init; }

    public required SectionBodyItem Item { get; init; }

    public int InsertIndex { get; init; }

    public Grid? DestGrid { get; init; }
}

/// <summary>What the canvas <c>+</c> menu should insert into a container.</summary>
public enum ReportDesignAddKind
{
    Subsection,
    Card,
    Table,
    Grid,
    Block
}

/// <summary>A canvas <c>+</c> action: insert into <see cref="Section" /> (and optional <see cref="Grid" />).</summary>
public sealed class ReportDesignAddRequest
{
    public required Section Section { get; init; }

    public ReportDesignAddKind Kind { get; init; }

    public ContentType? BlockType { get; init; }

    public Grid? Grid { get; init; }
}

/// <summary>Maps canvas/tree drop coordinates onto <see cref="SectionBody.Relocate" />.</summary>
public static class ReportDesignDropMapper
{
    /// <summary>Walks sections and nested subsections in document order.</summary>
    public static IEnumerable<Section> Walk(IEnumerable<Section> roots) => SectionBody.WalkSections(roots);

    /// <summary>Returns <see cref="Section.Id" /> when set. Does not invent or persist a designer id.</summary>
    public static string? TryId(Section section)
    {
        ArgumentHelpers.ThrowIfNull(section);
        return string.IsNullOrWhiteSpace(section.Id) ? null : section.Id;
    }

    /// <summary>Assigns a short id when <see cref="Section.Id" /> is empty and returns it. Prefer <see cref="ReportDesignTreeExpand.LiveKey" /> for designer chrome.</summary>
    public static string EnsureId(Section section)
    {
        ArgumentHelpers.ThrowIfNull(section);
        if (string.IsNullOrWhiteSpace(section.Id))
            section.Id = Guid.NewGuid().ToString("N")[..8];

        return section.Id!;
    }

    /// <summary>Finds a section by <see cref="Section.Id" />.</summary>
    public static Section? FindById(IEnumerable<Section> roots, string id)
    {
        ArgumentHelpers.ThrowIfNull(roots);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(id);
        foreach (var section in Walk(roots)) {
            if (string.Equals(section.Id, id, StringComparison.Ordinal))
                return section;
        }

        return null;
    }

    /// <summary>
    /// Relocates <paramref name="item" /> into the section with <paramref name="destId" /> at <paramref name="insertIndex" />.
    /// Returns false when the destination is missing or the move would cycle a subsection.
    /// </summary>
    public static bool TryRelocate(IEnumerable<Section> roots, SectionBodyItem item, string destId, int insertIndex)
    {
        ArgumentHelpers.ThrowIfNull(roots);
        ArgumentHelpers.ThrowIfNull(item);
        var dest = FindById(roots, destId);
        return dest is not null && SectionBody.Relocate(item.Section, dest, item, insertIndex);
    }
}

/// <summary>Tree expand keys for the design workbench. Chevron state is a set of these strings.</summary>
public static class ReportDesignTreeExpand
{
    private static readonly ConditionalWeakTable<object, StrongBox<string>> LiveKeys = new();

    /// <summary>Session-only identity for tree/canvas chrome. Does not write <see cref="Section.Id" />.</summary>
    public static string LiveKey(object node)
    {
        ArgumentHelpers.ThrowIfNull(node);
        return LiveKeys.GetValue(node, static _ => new StrongBox<string>(Guid.NewGuid().ToString("N")[..8])).Value!;
    }

    public static string SectionKey(Section section) => "s:" + LiveKey(section);

    /// <summary>Key for the report chrome (title), not a section.</summary>
    public static string ReportKey => "r";

    public static string ControlKey(Section section, Control control)
    {
        ArgumentHelpers.ThrowIfNull(section);
        ArgumentHelpers.ThrowIfNull(control);
        return "x:" + LiveKey(section) + ":" + LiveKey(control);
    }

    public static string CardKey(Section section, Card card) => ControlKey(section, card);

    public static string BlockKey(Section section, Block block) => ControlKey(section, block);

    public static string TableKey(Section section, Table table) => ControlKey(section, table);

    public static string GridKey(Section section, Grid grid) => ControlKey(section, grid);

    public static string TableColumnKey(Section section, Table table, TableColumn column)
    {
        ArgumentHelpers.ThrowIfNull(table);
        ArgumentHelpers.ThrowIfNull(column);
        return TableKey(section, table) + ":col:" + LiveKey(column);
    }

    public static string TableRowKey(Section section, Table table, TableRow row)
    {
        ArgumentHelpers.ThrowIfNull(table);
        ArgumentHelpers.ThrowIfNull(row);
        return TableKey(section, table) + ":row:" + LiveKey(row);
    }

    /// <summary>Key for a mixed body item (canvas wrappers and tree rows share these).</summary>
    public static string BodyItemKey(SectionBodyItem item)
        => item.Kind switch {
            SectionBodyKind.Control when item.Control is not null => ControlKey(item.Section, item.Control),
            SectionBodyKind.Subsection when item.Subsection is not null => SectionKey(item.Subsection),
            _ => "item"
        };

    /// <summary>
    /// Keys that must be expanded so <paramref name="selection" /> is visible, including the containing section (top-level included)
    /// so a collapsed parent reopens when the canvas hits a nested control.
    /// </summary>
    public static IReadOnlyList<string> AncestorKeys(IEnumerable<Section> roots, ReportDesignSelection selection)
    {
        ArgumentHelpers.ThrowIfNull(roots);
        ArgumentHelpers.ThrowIfNull(selection);
        var keys = new List<string>();
        if (selection.Section is null)
            return keys;

        foreach (var top in roots) {
            if (Collect(top, selection, keys))
                break;
        }

        return keys;
    }

    private static bool Collect(Section section, ReportDesignSelection selection, List<string> keys)
    {
        if (ContainsSelection(section, selection)) {
            keys.Add(SectionKey(section));
            if (selection.Grid is not null)
                keys.Add(GridKey(section, selection.Grid));
            else if (selection.Control is Grid g)
                keys.Add(GridKey(section, g));
            else if (selection.Table is not null)
                keys.Add(TableKey(section, selection.Table));
            else if (selection.TableColumn is not null || selection.TableRow is not null) {
                foreach (var table in SectionBody.WalkControls(section.Controls).OfType<Table>()) {
                    if ((selection.TableColumn is not null && table.Columns.Contains(selection.TableColumn))
                        || (selection.TableRow is not null && table.Rows.Contains(selection.TableRow))) {
                        keys.Add(TableKey(section, table));
                        break;
                    }
                }
            }

            foreach (var child in section.Subsections)
                Collect(child, selection, keys);

            return true;
        }

        foreach (var child in section.Subsections) {
            if (!Collect(child, selection, keys))
                continue;

            keys.Add(SectionKey(section));
            return true;
        }

        return false;
    }

    private static bool ContainsSelection(Section section, ReportDesignSelection selection)
    {
        if (ReferenceEquals(section, selection.Section))
            return true;

        var selected = selection.Control ?? (Control?)selection.Card ?? selection.Block ?? selection.Table ?? (Control?)selection.Grid;
        if (selected is not null && SectionBody.WalkControls(section.Controls).Contains(selected))
            return true;

        foreach (var table in SectionBody.WalkControls(section.Controls).OfType<Table>()) {
            if (selection.TableColumn is not null && table.Columns.Contains(selection.TableColumn))
                return true;

            if (selection.TableRow is not null && table.Rows.Contains(selection.TableRow))
                return true;
        }

        return false;
    }
}
