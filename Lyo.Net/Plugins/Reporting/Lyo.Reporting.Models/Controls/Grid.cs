using System.Diagnostics;
using System.Globalization;
using Lyo.Exceptions;

namespace Lyo.Reporting.Models.Controls;

/// <summary>CSS grid that lays out <see cref="Card" />, <see cref="Block" />, and <see cref="Table" /> children. Nested grids are not allowed.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class Grid : Control
{
    /// <summary>Optional title above the grid.</summary>
    public string? Title { get; set; }

    /// <summary>Number of equal columns when <see cref="TemplateColumns" /> is not a CSS track list. Default 2.</summary>
    public int ColumnCount { get; set; } = 2;

    /// <summary>Optional CSS <c>grid-template-columns</c> override (for example <c>1fr 2fr</c> or <c>repeat(auto-fit, minmax(220px, 1fr))</c>). Empty or a bare integer uses <see cref="ColumnCount" />.</summary>
    public string? TemplateColumns { get; set; }

    /// <summary>CSS gap. Paint uses 16px when unset.</summary>
    public string? Gap { get; set; }

    /// <summary>Child controls in auto-flow order. Must not contain a <see cref="Grid" />.</summary>
    public List<Control> Controls { get; set; } = [];

    /// <summary>
    /// CSS <c>grid-template-columns</c> value. A real track list on <see cref="TemplateColumns" /> wins; empty, whitespace, or a bare integer uses
    /// <c>repeat(ColumnCount, 1fr)</c>.
    /// </summary>
    public string ResolveTemplateColumns()
    {
        var columns = Math.Max(ColumnCount, 1);
        var raw = TemplateColumns?.Trim();
        if (string.IsNullOrEmpty(raw) || !LooksLikeCssTrackList(raw))
            return $"repeat({columns}, 1fr)";

        return raw;
    }

    /// <summary>Appends a child. Rejects a nested <see cref="Grid" />.</summary>
    public bool TryAdd(Control control)
    {
        ArgumentHelpers.ThrowIfNull(control);
        if (control is Grid)
            return false;

        Controls.Add(control);
        return true;
    }

    public override string ToString() => $"Grid: {Title ?? "(Untitled)"} ({ColumnCount} cols, {Controls.Count} items)";

    private static bool LooksLikeCssTrackList(string value)
    {
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
            return false;

        var lower = value.ToLowerInvariant();
        return lower.Contains("fr", StringComparison.Ordinal)
            || lower.Contains("repeat(", StringComparison.Ordinal)
            || lower.Contains("minmax(", StringComparison.Ordinal)
            || lower.Contains("px", StringComparison.Ordinal)
            || lower.Contains("%", StringComparison.Ordinal)
            || lower.Contains("auto", StringComparison.Ordinal);
    }
}
