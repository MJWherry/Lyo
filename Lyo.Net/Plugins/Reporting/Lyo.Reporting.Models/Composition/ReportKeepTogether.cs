using Lyo.Exceptions;
using Lyo.Reporting.Models.Controls;

namespace Lyo.Reporting.Models.Composition;

/// <summary>
/// Print/PDF keep-together: whether a control should avoid splitting across pages.
/// Null <c>KeepTogether</c> on the model means the type default — on for cards, blocks, and grids; off for tables. Page breaks never keep together.
/// </summary>
public static class ReportKeepTogether
{
    /// <summary>Inline CSS appended when a control is kept together.</summary>
    public const string Css = "break-inside: avoid; page-break-inside: avoid";

    /// <summary>True when the control should stay on one page.</summary>
    public static bool Effective(Control control)
    {
        ArgumentHelpers.ThrowIfNull(control);
        return control switch {
            Block { ContentType: ContentType.PageBreak } => false,
            Table table => table.KeepTogether ?? false,
            _ => control.KeepTogether ?? true
        };
    }

    /// <summary>True when the block should stay on one page. <see cref="ContentType.PageBreak" /> always returns false.</summary>
    public static bool Effective(Block block)
    {
        ArgumentHelpers.ThrowIfNull(block);
        return Effective((Control)block);
    }

    /// <summary>True when the table should stay on one page. Null defaults to off so tall tables can split.</summary>
    public static bool Effective(Table table)
    {
        ArgumentHelpers.ThrowIfNull(table);
        return table.KeepTogether ?? false;
    }

    /// <summary>True when the grid should stay on one page. Null defaults to on.</summary>
    public static bool Effective(Grid grid)
    {
        ArgumentHelpers.ThrowIfNull(grid);
        return grid.KeepTogether ?? true;
    }
}
