using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Web.Primitives;

/// <summary>
/// Renders a status string as a chip, taking the color, icon, and label from the domain <see cref="ILyoStatusPalette" /> named by <see cref="Palette" /> and falling
/// back to the shared vocabulary in <see cref="LyoDefaultStatusPalette" />.
/// </summary>
/// <remarks>
/// Use it instead of hand-writing <c>MudChip</c> markup around a <c>*ColorHelper</c> call, so a status looks the same on a grid cell, a detail view, and a dialog
/// header. Any of <see cref="Label" />, <see cref="Color" />, and <see cref="Icon" /> set explicitly wins over the palette, which is how a component keeps a
/// one-off presentation without leaving the primitive.
/// <code>
/// &lt;LyoStatusChip Status="@run.Result" Palette="job"/&gt;
/// &lt;LyoStatusChip Status="@message.Status" Palette="sms" ShowIcon="false"/&gt;
/// </code>
/// </remarks>
public partial class LyoStatusChip
{
    /// <summary>Raw status text, in any casing and with either underscores or hyphens.</summary>
    [Parameter]
    [EditorRequired]
    public string? Status { get; set; }

    /// <summary>Domain palette to consult first, for example <c>job</c>. Leave unset to use only the shared vocabulary.</summary>
    [Parameter]
    public string? Palette { get; set; }

    /// <summary>Overrides the palette label. Useful when the stored status is an internal code.</summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>Overrides the palette color.</summary>
    [Parameter]
    public Color? Color { get; set; }

    /// <summary>Overrides the palette icon. Ignored when <see cref="ShowIcon" /> is false.</summary>
    [Parameter]
    public string? Icon { get; set; }

    /// <summary>Drop the icon and show text only, for dense cells where the icon crowds the label.</summary>
    [Parameter]
    public bool ShowIcon { get; set; } = true;

    /// <summary>Overrides the palette variant.</summary>
    [Parameter]
    public Variant? Variant { get; set; }

    /// <summary>Defaults to <see cref="MudBlazor.Size.Small" />, matching <see cref="LyoChip" />.</summary>
    [Parameter]
    public Size Size { get; set; } = Size.Small;

    /// <summary>Optional click handler, for chips that double as a filter toggle.</summary>
    [Parameter]
    public EventCallback OnClick { get; set; }

    /// <summary>Inline style forwarded onto the chip.</summary>
    [Parameter]
    public string? Style { get; set; }

    /// <summary>CSS class forwarded onto the chip.</summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>HTML <c>title</c> tooltip. Defaults to the raw status, so a shortened label still reveals what was stored.</summary>
    [Parameter]
    public string? Title { get; set; }

    [Inject]
    private IServiceProvider Services { get; set; } = null!;

    private LyoStatusPaletteResolver Resolver => Services.GetService<LyoStatusPaletteResolver>() ?? LyoStatusPaletteResolver.Default;

    /// <summary>Palette result with the explicit parameters applied, so the chip never re-derives the icon and re-introduces it after <see cref="ShowIcon" /> hid it.</summary>
    private LyoChipSpec ResolvedSpec
    {
        get
        {
            var spec = Resolver.Resolve(Status, Palette);
            return new LyoChipSpec(
                Label ?? spec.Label,
                Color ?? spec.Color,
                ShowIcon ? Icon ?? spec.Icon : null,
                Variant ?? spec.Variant,
                spec.Style);
        }
    }

    private string? ResolvedTitle => Title ?? Status;
}
