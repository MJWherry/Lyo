using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Primitives;

/// <summary>Vertical rule between groups or actions in a <see cref="LyoToolbar" />.</summary>
public partial class LyoToolbarSeparator
{
    /// <summary>CSS class forwarded onto the rule.</summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>Inline style forwarded onto the rule.</summary>
    [Parameter]
    public string? Style { get; set; }
}
