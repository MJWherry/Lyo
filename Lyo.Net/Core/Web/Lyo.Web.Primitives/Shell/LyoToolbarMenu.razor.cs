using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Primitives;

/// <summary>
/// Compact menu for a <see cref="LyoToolbar" />. A <see cref="Label" /> uses a dropdown chevron; icon-only menus omit it. Origins and popover placement are typed parameters; remaining attributes (for example <c>MaxHeight</c>) are forwarded onto <c>MudMenu</c>.
/// </summary>
public partial class LyoToolbarMenu
{
    /// <summary>Activator label. When set, the activator is a button with a dropdown chevron.</summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>Activator icon. Start icon when <see cref="Label" /> is set; the whole activator when it is not.</summary>
    [Parameter]
    public string? Icon { get; set; }

    /// <summary>MudBlazor color on the activator. Default is <see cref="Color.Default" />.</summary>
    [Parameter]
    public Color Color { get; set; } = Color.Default;

    /// <summary>MudBlazor variant on a labeled activator. Outlined by default.</summary>
    [Parameter]
    public Variant Variant { get; set; } = Variant.Outlined;

    /// <summary>Disables the menu. Also disabled when the owning toolbar is disabled.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Tooltip on the activator.</summary>
    [Parameter]
    public string? Tooltip { get; set; }

    /// <summary>Dense menu list. On by default.</summary>
    [Parameter]
    public bool Dense { get; set; } = true;

    /// <summary>Menu items, nested menus, or other popover content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>CSS class forwarded onto the menu root.</summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>
    /// Popover anchor. Typed here because unmatched Razor attributes are strings, and MudBlazor 9's <c>MudMenu.AnchorOrigin</c> is
    /// <see cref="Origin" />?.
    /// </summary>
    [Parameter]
    public Origin AnchorOrigin { get; set; } = Origin.TopLeft;

    /// <summary>Popover transform origin. Same typing reason as <see cref="AnchorOrigin" />.</summary>
    [Parameter]
    public Origin TransformOrigin { get; set; } = Origin.TopRight;

    /// <summary>When true, the popover is <c>position: fixed</c> (filter panels that must escape overflow).</summary>
    [Parameter]
    public bool PopoverFixed { get; set; }

    /// <summary>Extra CSS class on the popover paper.</summary>
    [Parameter]
    public string? PopoverClass { get; set; }

    /// <summary>Attributes forwarded onto <c>MudMenu</c>.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    [CascadingParameter]
    private LyoToolbar? Owner { get; set; }

    private bool IsDisabled => LyoToolbarLayout.IsDisabled(Disabled, Owner?.Disabled ?? false);

    private bool HasLabel => !string.IsNullOrWhiteSpace(Label);
}
