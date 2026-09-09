using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Primitives;

/// <summary>
/// Placeholder rows shown while a panel loads, so the layout keeps its height instead of collapsing and then jumping when data arrives. Prefer it over a spinner for
/// anything list- or table-shaped.
/// </summary>
/// <remarks>
/// Row widths taper slightly so the block reads as text rather than a solid rectangle. <see cref="LyoResultBoundary{T}" /> renders one of these by default.
/// </remarks>
public partial class LyoSkeletonPanel
{
    /// <summary>Number of placeholder rows. Match the page size the real content will have, roughly.</summary>
    [Parameter]
    public int Rows { get; set; } = 4;

    /// <summary>Adds a wider first bar standing in for a heading or toolbar.</summary>
    [Parameter]
    public bool ShowHeader { get; set; }

    /// <summary>Shape of each row. Rectangles suit cards and images, text suits lists and tables.</summary>
    [Parameter]
    public SkeletonType SkeletonType { get; set; } = SkeletonType.Text;

    /// <summary>CSS height of each row, for example <c>1.5rem</c> for text or <c>4rem</c> for cards.</summary>
    [Parameter]
    public string RowHeight { get; set; } = "1.5rem";

    /// <summary>CSS height of the header bar when <see cref="ShowHeader" /> is set.</summary>
    [Parameter]
    public string HeaderHeight { get; set; } = "2rem";

    /// <summary>Shimmer style. <c>Animation.False</c> removes motion for reduced-motion contexts.</summary>
    [Parameter]
    public Animation Animation { get; set; } = Animation.Wave;

    /// <summary>CSS class forwarded onto the wrapper.</summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>Inline style forwarded onto the wrapper.</summary>
    [Parameter]
    public string? Style { get; set; }

    /// <summary>Cycles through a few widths so consecutive rows do not line up into a block.</summary>
    private string RowWidth(int row)
        => (row % 3) switch {
            0 => "100%",
            1 => "92%",
            var _ => "78%"
        };
}
