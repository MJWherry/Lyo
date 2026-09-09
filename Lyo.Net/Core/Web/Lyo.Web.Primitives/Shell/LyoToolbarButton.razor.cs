using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Primitives;

/// <summary>
/// Compact outlined button for a <see cref="LyoToolbar" />. Size is always small. <see cref="Disabled" /> ORs with the owning toolbar's cascade.
/// Extra attributes are forwarded onto <c>MudButton</c>.
/// </summary>
public partial class LyoToolbarButton
{
    /// <summary>Button label.</summary>
    [Parameter]
    public string? Text { get; set; }

    /// <summary>Optional start icon.</summary>
    [Parameter]
    public string? Icon { get; set; }

    /// <summary>MudBlazor color. Default is <see cref="Color.Default" />.</summary>
    [Parameter]
    public Color Color { get; set; } = Color.Default;

    /// <summary>MudBlazor variant. Outlined by default; use Filled for the primary action.</summary>
    [Parameter]
    public Variant Variant { get; set; } = Variant.Outlined;

    /// <summary>Disables this button. Also disabled when the owning toolbar is disabled.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Click handler.</summary>
    [Parameter]
    public EventCallback OnClick { get; set; }

    /// <summary>Tooltip. When set, the button is wrapped in <c>MudTooltip</c>.</summary>
    [Parameter]
    public string? Tooltip { get; set; }

    /// <summary>CSS class forwarded onto the button.</summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>Attributes forwarded onto <c>MudButton</c>.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    [CascadingParameter]
    private LyoToolbar? Owner { get; set; }

    private bool IsDisabled => LyoToolbarLayout.IsDisabled(Disabled, Owner?.Disabled ?? false);
}
