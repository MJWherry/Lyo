using Lyo.Exceptions;
using Lyo.Reporting.Models.Controls;
using Lyo.Reporting.Models.Models;

namespace Lyo.Reporting.Models.Composition;

/// <summary>Kind of mixed body entry inside a <see cref="Section" />.</summary>
public enum SectionBodyKind
{
    Control,
    Subsection
}

/// <summary>One entry in a section's mixed paint/designer sequence.</summary>
public sealed class SectionBodyItem
{
    public SectionBodyKind Kind { get; init; }

    public Section Section { get; init; } = null!;

    public Control? Control { get; init; }

    public Section? Subsection { get; init; }

    public bool SameAs(SectionBodyItem other)
    {
        ArgumentHelpers.ThrowIfNull(other);
        if (Kind != other.Kind)
            return false;

        return Kind switch {
            SectionBodyKind.Control => ReferenceEquals(Control, other.Control),
            SectionBodyKind.Subsection => ReferenceEquals(Subsection, other.Subsection),
            _ => false
        };
    }
}

/// <summary>
/// Mixed body order for section controls and subsections. All-zero <c>Order</c> keeps controls in list order, then subsections.
/// </summary>
public static class SectionBody
{
    /// <summary>True when the section has never been assigned mixed orders (all zeros).</summary>
    public static bool IsLegacy(Section section)
    {
        ArgumentHelpers.ThrowIfNull(section);
        return section.Controls.TrueForAll(c => c.Order == 0) && section.Subsections.TrueForAll(s => s.Order == 0);
    }

    /// <summary>Paint/designer sequence for <paramref name="section" />.</summary>
    public static IReadOnlyList<SectionBodyItem> Enumerate(Section section)
    {
        ArgumentHelpers.ThrowIfNull(section);
        return IsLegacy(section) ? EnumerateLegacy(section) : EnumerateOrdered(section);
    }

    /// <summary>Swaps <paramref name="item" /> with its sibling at <paramref name="delta" /> (-1 or +1) and writes 1-based orders.</summary>
    public static bool Move(Section section, SectionBodyItem item, int delta)
    {
        ArgumentHelpers.ThrowIfNull(section);
        ArgumentHelpers.ThrowIfNull(item);
        var items = Enumerate(section).ToList();
        var index = items.FindIndex(item.SameAs);
        if (index < 0)
            return false;

        var next = index + delta;
        if (next < 0 || next >= items.Count)
            return false;

        (items[index], items[next]) = (items[next], items[index]);
        WriteOrders(section, items);
        return true;
    }

    /// <summary>
    /// Moves <paramref name="item" /> from <paramref name="source" /> into <paramref name="dest" /> at <paramref name="insertIndex" />.
    /// Returns false when the move would nest a subsection under itself or place a grid inside a grid.
    /// </summary>
    public static bool Relocate(Section source, Section dest, SectionBodyItem item, int insertIndex)
    {
        ArgumentHelpers.ThrowIfNull(source);
        ArgumentHelpers.ThrowIfNull(dest);
        ArgumentHelpers.ThrowIfNull(item);
        if (item.Kind == SectionBodyKind.Subsection && item.Subsection is not null && Contains(item.Subsection, dest))
            return false;

        if (ReferenceEquals(source, dest))
            return MoveToIndex(source, item, insertIndex);

        Detach(source, item);
        Attach(dest, item);
        var destItems = Enumerate(dest).Where(i => !item.SameAs(i)).ToList();
        if (insertIndex < 0)
            insertIndex = 0;
        if (insertIndex > destItems.Count)
            insertIndex = destItems.Count;

        destItems.Insert(insertIndex, CloneFor(dest, item));
        WriteOrders(dest, destItems);
        WriteOrders(source, Enumerate(source));
        return true;
    }

    /// <summary>Moves a control from its current owner onto <paramref name="dest" /> at <paramref name="insertIndex" />. Nested grids are rejected.</summary>
    public static bool RelocateToGrid(Section source, Control control, Grid dest, int insertIndex)
    {
        ArgumentHelpers.ThrowIfNull(source);
        ArgumentHelpers.ThrowIfNull(control);
        ArgumentHelpers.ThrowIfNull(dest);
        if (control is Grid)
            return false;

        if (!TryOwner(source, control, out var owner) && !TryOwnerInSubsections(source, control, out owner))
            return false;

        if (ReferenceEquals(owner, dest.Controls))
            return MoveInList(dest.Controls, control, insertIndex);

        owner!.Remove(control);
        if (insertIndex < 0)
            insertIndex = 0;
        if (insertIndex > dest.Controls.Count)
            insertIndex = dest.Controls.Count;

        dest.Controls.Insert(insertIndex, control);
        WriteOrders(source, Enumerate(source));
        return true;
    }

    /// <summary>Swaps a table column and every row cell at that index.</summary>
    public static bool MoveTableColumn(Table table, int index, int delta)
        => MoveTableColumnTo(table, index, index + delta + (delta > 0 ? 1 : 0));

    /// <summary>Moves a table column (and lockstep cells) so it lands at <paramref name="insertIndex" /> in the pre-move list.</summary>
    public static bool MoveTableColumnTo(Table table, int fromIndex, int insertIndex)
    {
        ArgumentHelpers.ThrowIfNull(table);
        if (fromIndex < 0 || fromIndex >= table.Columns.Count)
            return false;

        if (insertIndex < 0)
            insertIndex = 0;
        if (insertIndex > table.Columns.Count)
            insertIndex = table.Columns.Count;

        if (insertIndex == fromIndex || insertIndex == fromIndex + 1)
            return true;

        var column = table.Columns[fromIndex];
        table.Columns.RemoveAt(fromIndex);
        var dest = insertIndex > fromIndex ? insertIndex - 1 : insertIndex;
        table.Columns.Insert(dest, column);
        foreach (var row in table.Rows) {
            while (row.Cells.Count <= fromIndex)
                row.Cells.Add(null);

            var cell = row.Cells[fromIndex];
            row.Cells.RemoveAt(fromIndex);
            while (row.Cells.Count < dest)
                row.Cells.Add(null);

            row.Cells.Insert(dest, cell);
        }

        return true;
    }

    /// <summary>Moves a control so it lands at <paramref name="insertIndex" /> in the owner's list.</summary>
    public static bool MoveControlTo(IList<Control> owner, Control control, int insertIndex)
    {
        ArgumentHelpers.ThrowIfNull(owner);
        ArgumentHelpers.ThrowIfNull(control);
        return MoveInList(owner, control, insertIndex);
    }

    /// <summary>True when <paramref name="root" /> is <paramref name="target" /> or contains it as a nested subsection.</summary>
    public static bool Contains(Section root, Section target)
    {
        ArgumentHelpers.ThrowIfNull(root);
        ArgumentHelpers.ThrowIfNull(target);
        if (ReferenceEquals(root, target))
            return true;

        foreach (var child in root.Subsections) {
            if (Contains(child, target))
                return true;
        }

        return false;
    }

    /// <summary>Writes 1-based mixed orders from <paramref name="items" /> and rebuilds <see cref="Section.Controls" /> in that sequence.</summary>
    public static void WriteOrders(Section section, IReadOnlyList<SectionBodyItem> items)
    {
        ArgumentHelpers.ThrowIfNull(section);
        ArgumentHelpers.ThrowIfNull(items);
        var controls = new List<Control>();
        var subsections = new List<Section>();
        var order = 1;
        foreach (var item in items) {
            WriteOrder(item, order++);
            if (item.Kind == SectionBodyKind.Control && item.Control is not null)
                controls.Add(item.Control);
            else if (item.Kind == SectionBodyKind.Subsection && item.Subsection is not null)
                subsections.Add(item.Subsection);
        }

        section.Controls = controls;
        section.Subsections = subsections;
    }

    /// <summary>All tables in <paramref name="sections" />, including those nested in grids and subsections.</summary>
    public static IReadOnlyList<Table> CollectTables(IEnumerable<Section> sections)
    {
        ArgumentHelpers.ThrowIfNull(sections);
        var tables = new List<Table>();
        foreach (var section in sections.OrderBy(s => s.Order)) {
            foreach (var control in WalkControls(section.Controls)) {
                if (control is Table table)
                    tables.Add(table);
            }

            tables.AddRange(CollectTables(section.Subsections));
        }

        return tables;
    }

    /// <summary>Depth-first walk of controls, including grid children.</summary>
    public static IEnumerable<Control> WalkControls(IEnumerable<Control> controls)
    {
        ArgumentHelpers.ThrowIfNull(controls);
        foreach (var control in controls) {
            yield return control;
            if (control is not Grid grid)
                continue;

            foreach (var child in WalkControls(grid.Controls))
                yield return child;
        }
    }

    /// <summary>Walks sections and nested subsections in document order.</summary>
    public static IEnumerable<Section> WalkSections(IEnumerable<Section> roots)
    {
        ArgumentHelpers.ThrowIfNull(roots);
        foreach (var section in roots) {
            yield return section;
            foreach (var nested in WalkSections(section.Subsections))
                yield return nested;
        }
    }

    /// <summary>Finds the list that currently owns <paramref name="control" /> under <paramref name="section" /> (section body or a grid).</summary>
    public static bool TryOwner(Section section, Control control, out List<Control>? owner)
    {
        ArgumentHelpers.ThrowIfNull(section);
        ArgumentHelpers.ThrowIfNull(control);
        if (section.Controls.Contains(control)) {
            owner = section.Controls;
            return true;
        }

        foreach (var grid in WalkControls(section.Controls).OfType<Grid>()) {
            if (!grid.Controls.Contains(control))
                continue;

            owner = grid.Controls;
            return true;
        }

        owner = null;
        return false;
    }

    private static bool TryOwnerInSubsections(Section section, Control control, out List<Control>? owner)
    {
        foreach (var nested in WalkSections(section.Subsections)) {
            if (TryOwner(nested, control, out owner))
                return true;
        }

        owner = null;
        return false;
    }

    private static bool MoveToIndex(Section section, SectionBodyItem item, int insertIndex)
    {
        var items = Enumerate(section).ToList();
        var current = items.FindIndex(item.SameAs);
        if (current < 0)
            return false;

        items.RemoveAt(current);
        if (insertIndex > current)
            insertIndex--;
        if (insertIndex < 0)
            insertIndex = 0;
        if (insertIndex > items.Count)
            insertIndex = items.Count;

        items.Insert(insertIndex, item);
        WriteOrders(section, items);
        return true;
    }

    private static bool MoveInList(IList<Control> owner, Control control, int insertIndex)
    {
        var fromIndex = IndexOf(owner, control);
        if (fromIndex < 0)
            return false;

        if (insertIndex < 0)
            insertIndex = 0;
        if (insertIndex > owner.Count)
            insertIndex = owner.Count;

        if (insertIndex == fromIndex || insertIndex == fromIndex + 1)
            return true;

        owner.RemoveAt(fromIndex);
        var dest = insertIndex > fromIndex ? insertIndex - 1 : insertIndex;
        owner.Insert(dest, control);
        return true;
    }

    private static int IndexOf(IList<Control> owner, Control control)
    {
        for (var i = 0; i < owner.Count; i++) {
            if (ReferenceEquals(owner[i], control))
                return i;
        }

        return -1;
    }

    private static void WriteOrder(SectionBodyItem item, int order)
    {
        switch (item.Kind) {
            case SectionBodyKind.Control when item.Control is not null:
                item.Control.Order = order;
                break;
            case SectionBodyKind.Subsection when item.Subsection is not null:
                item.Subsection.Order = order;
                break;
        }
    }

    private static void Detach(Section source, SectionBodyItem item)
    {
        switch (item.Kind) {
            case SectionBodyKind.Control when item.Control is not null:
                if (TryOwner(source, item.Control, out var owner))
                    owner!.Remove(item.Control);
                break;
            case SectionBodyKind.Subsection when item.Subsection is not null:
                source.Subsections.Remove(item.Subsection);
                break;
        }
    }

    private static void Attach(Section dest, SectionBodyItem item)
    {
        switch (item.Kind) {
            case SectionBodyKind.Control when item.Control is not null && !dest.Controls.Contains(item.Control):
                dest.Controls.Add(item.Control);
                break;
            case SectionBodyKind.Subsection when item.Subsection is not null && !dest.Subsections.Contains(item.Subsection):
                dest.Subsections.Add(item.Subsection);
                break;
        }
    }

    private static SectionBodyItem CloneFor(Section dest, SectionBodyItem item)
        => new() { Kind = item.Kind, Section = dest, Control = item.Control, Subsection = item.Subsection };

    private static List<SectionBodyItem> EnumerateLegacy(Section section)
    {
        var items = new List<SectionBodyItem>();
        foreach (var control in section.Controls)
            items.Add(ControlItem(section, control));

        foreach (var sub in section.Subsections.Select((s, i) => (Section: s, Index: i)).OrderBy(x => x.Section.Order).ThenBy(x => x.Index))
            items.Add(SubItem(section, sub.Section));

        return items;
    }

    private static List<SectionBodyItem> EnumerateOrdered(Section section)
    {
        var raw = new List<(int Order, int Index, SectionBodyItem Item)>();
        var index = 0;
        foreach (var control in section.Controls)
            raw.Add((control.Order, index++, ControlItem(section, control)));

        foreach (var sub in section.Subsections)
            raw.Add((sub.Order, index++, SubItem(section, sub)));

        return raw.OrderBy(x => x.Order).ThenBy(x => x.Index).Select(x => x.Item).ToList();
    }

    private static SectionBodyItem ControlItem(Section section, Control control)
        => new() { Kind = SectionBodyKind.Control, Section = section, Control = control };

    private static SectionBodyItem SubItem(Section section, Section subsection)
        => new() { Kind = SectionBodyKind.Subsection, Section = section, Subsection = subsection };
}
