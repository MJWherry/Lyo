using Lyo.Web.Components.Popover;
using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Components.DataGrid;

/// <summary>
/// Ellipsized text that reveals the full value in a <see cref="Popover.LyoHoverPopover" /> on hover. The popover stays open while the pointer is over it, so long values can
/// be read and copied instead of vanishing like a tooltip.
/// </summary>
public partial class LyoTruncatedText
{
    /// <summary>Full text. Truncated in-cell; hovering reveals this value in a popover when longer than <see cref="MaxLength" />.</summary>
    [Parameter]
    public string? Text { get; set; }

    /// <summary>Visible character budget including the ellipsis. Default is <see cref="ChipLabelHelper.DefaultGridCellMaxLength" />.</summary>
    [Parameter]
    public int MaxLength { get; set; } = ChipLabelHelper.DefaultGridCellMaxLength;

    /// <summary>Optional heading for the popover, for example the column title.</summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>CSS max-width for the popover content.</summary>
    [Parameter]
    public string PopoverMaxWidth { get; set; } = "32rem";
}
