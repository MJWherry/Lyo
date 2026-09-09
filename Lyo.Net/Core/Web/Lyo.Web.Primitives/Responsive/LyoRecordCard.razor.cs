using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Primitives;

/// <summary>
/// One record as a card: an optional heading, an actions slot, and <see cref="LyoRecordField" /> children for the label/value pairs. Matches the cards the shared
/// data grids render, so hand-rolled tables wrapped in <see cref="LyoResponsiveTable" /> look the same on a phone as the grids do.
/// </summary>
public partial class LyoRecordCard
{
    /// <summary>Heading text, usually whatever the first column of the table shows.</summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>Secondary line under the title, for example an identifier or a timestamp.</summary>
    [Parameter]
    public string? Subtitle { get; set; }

    /// <summary>Extra CSS classes for the title text, for example the host's monospace class for keys and identifiers.</summary>
    [Parameter]
    public string? TitleClass { get; set; }

    /// <summary>Markup for the heading, rendered after <see cref="Title" /> and <see cref="Subtitle" />. Use for chips and status icons.</summary>
    [Parameter]
    public RenderFragment? TitleContent { get; set; }

    /// <summary>Buttons or a menu shown at the top right, matching the actions the table row already offers.</summary>
    [Parameter]
    public RenderFragment? Actions { get; set; }

    /// <summary>Card body: a sequence of <see cref="LyoRecordField" /> elements.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Highlights the card, for example for the selected or currently applied record.</summary>
    [Parameter]
    public bool Selected { get; set; }

    /// <summary>Extra CSS classes for the card surface.</summary>
    [Parameter]
    public string? Class { get; set; }

    private string TitleCssClass => string.IsNullOrWhiteSpace(TitleClass) ? "lyo-record-card-title" : $"lyo-record-card-title {TitleClass}";

    private bool HasHeader => !string.IsNullOrWhiteSpace(Title) || !string.IsNullOrWhiteSpace(Subtitle) || TitleContent is not null || Actions is not null;

    private string CardCssClass
    {
        get {
            var css = Selected ? "lyo-record-card lyo-record-card-selected" : "lyo-record-card";
            return string.IsNullOrWhiteSpace(Class) ? css : $"{css} {Class}";
        }
    }
}
