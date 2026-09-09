using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Primitives;

/// <summary>
/// Compact icon-only button for a <see cref="LyoToolbar" />. Uses a 28px hit target. <see cref="Disabled" /> ORs with the owning toolbar's cascade.
/// Extra attributes are forwarded onto <c>MudIconButton</c>.
/// </summary>
public partial class LyoToolbarIconButton
{
    /// <summary>Material icon.</summary>
    [Parameter]
    [EditorRequired]
    public string Icon { get; set; } = string.Empty;

    /// <summary>MudBlazor color. Default is <see cref="Color.Default" />.</summary>
    [Parameter]
    public Color Color { get; set; } = Color.Default;

    /// <summary>Disables this button. Also disabled when the owning toolbar is disabled.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Click handler.</summary>
    [Parameter]
    public EventCallback OnClick { get; set; }

    /// <summary>Tooltip. When set, wraps in <c>MudTooltip</c>; otherwise applied as <c>title</c>.</summary>
    [Parameter]
    public string? Tooltip { get; set; }

    /// <summary>CSS class forwarded onto the button.</summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>Attributes forwarded onto <c>MudIconButton</c>.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    [CascadingParameter]
    private LyoToolbar? Owner { get; set; }

    private bool IsDisabled => LyoToolbarLayout.IsDisabled(Disabled, Owner?.Disabled ?? false);
}
