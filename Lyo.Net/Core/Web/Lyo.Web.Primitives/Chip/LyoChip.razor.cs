using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Primitives;

public partial class LyoChip
{
    /// <summary>Pre-built spec. When set, supplies label, color, icon, variant, and style unless the matching parameter is also set.</summary>
    [Parameter]
    public LyoChipSpec? Spec { get; set; }

    /// <summary>Chip text when <see cref="ChildContent" /> is unset. Falls back to <see cref="Spec" />, then an em dash.</summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>Alias for <see cref="Label" /> (MudChip <c>Text</c>).</summary>
    [Parameter]
    public string? Text { get; set; }

    /// <summary>MudBlazor color. Falls back to <see cref="Spec" /> when unset.</summary>
    [Parameter]
    public Color? Color { get; set; }

    /// <summary>Optional Material icon shown beside the title. Falls back to <see cref="Spec" /> when unset.</summary>
    [Parameter]
    public string? Icon { get; set; }

    /// <summary>Filled by default. Falls back to <see cref="Spec" /> when unset.</summary>
    [Parameter]
    public Variant? Variant { get; set; }

    /// <summary>Defaults to <see cref="Size.Small" />.</summary>
    [Parameter]
    public Size Size { get; set; } = Size.Small;

    /// <summary>Optional click handler (enabled/active toggles and filter chips).</summary>
    [Parameter]
    public EventCallback OnClick { get; set; }

    /// <summary>When set, MudBlazor shows a close icon.</summary>
    [Parameter]
    public EventCallback OnClose { get; set; }

    /// <summary>Close icon used when <see cref="OnClose" /> is set.</summary>
    [Parameter]
    public string? CloseIcon { get; set; }

    /// <summary>Inline style forwarded onto the chip.</summary>
    [Parameter]
    public string? Style { get; set; }

    /// <summary>CSS class forwarded onto the chip.</summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>HTML <c>title</c> tooltip.</summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>Custom inner markup (for example <c>LyoTimestamp</c>). Replaces <see cref="Label" />.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private string ResolvedLabel
        => !string.IsNullOrWhiteSpace(Label) ? Label
            : !string.IsNullOrWhiteSpace(Text) ? Text
            : Spec?.Label ?? "—";

    private Color ResolvedColor => Color ?? Spec?.Color ?? MudBlazor.Color.Default;

    private string? ResolvedIcon => Icon ?? Spec?.Icon;

    private Variant ResolvedVariant => Variant ?? Spec?.Variant ?? MudBlazor.Variant.Filled;

    private string? ResolvedStyle
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Style))
                return Spec?.Style;
            if (string.IsNullOrWhiteSpace(Spec?.Style))
                return Style;
            return Spec.Style + ";" + Style;
        }
    }

    private string? ResolvedClass
    {
        get
        {
            var hue = ResolvedStyle is not null && ResolvedStyle.Contains(LyoChipHue.CssVariable, StringComparison.Ordinal);
            if (!hue)
                return Class;
            return string.IsNullOrWhiteSpace(Class) ? LyoChipHue.CssClass : Class + " " + LyoChipHue.CssClass;
        }
    }
}
