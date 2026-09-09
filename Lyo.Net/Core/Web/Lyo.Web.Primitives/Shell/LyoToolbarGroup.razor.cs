using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Primitives;

/// <summary>
/// Inner flex cluster inside a <see cref="LyoToolbarRow" /> or a shorthand <see cref="LyoToolbar" /> slot. Use <see cref="Grow" /> for search fields and
/// <see cref="AlignEnd" /> for trailing actions.
/// </summary>
public partial class LyoToolbarGroup
{
    /// <summary>Controls in this group.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Takes leftover width on the row.</summary>
    [Parameter]
    public bool Grow { get; set; }

    /// <summary>Pins the group to the inline end until the row wraps.</summary>
    [Parameter]
    public bool AlignEnd { get; set; }

    /// <summary>CSS class forwarded onto the group.</summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>Inline style forwarded onto the group.</summary>
    [Parameter]
    public string? Style { get; set; }

    private string GroupCssClass => LyoToolbarLayout.Group(Grow, AlignEnd);
}
