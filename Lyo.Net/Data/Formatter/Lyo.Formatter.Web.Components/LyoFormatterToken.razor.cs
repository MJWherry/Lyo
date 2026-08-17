using Lyo.Formatter;
using Microsoft.AspNetCore.Components;

namespace Lyo.Formatter.Web.Components;

/// <summary>Colored span for one <see cref="FormatterSegment" />. Used by the template overlay and the preview; not a host-facing layout component.</summary>
public partial class LyoFormatterToken
{
    [Parameter]
    [EditorRequired]
    public FormatterSegment Segment { get; set; } = default!;

    [Parameter]
    [EditorRequired]
    public LyoFormatterLiveSession Session { get; set; } = default!;

    [Parameter]
    public bool ShowRawToken { get; set; }

    private bool Unresolved => Segment.Kind == FormatterSegmentKind.Unresolved;

    private bool Hovered => Session.IsHovered(Segment.PlaceholderKey);

    private string DisplayText => ShowRawToken ? Segment.RawToken ?? Segment.Text : Segment.Text;

    private string TokenClass {
        get {
            var css = "lyo-fmt-token";
            if (Unresolved)
                css += " lyo-fmt-token-unresolved";
            if (Hovered)
                css += " lyo-fmt-token-active";
            return css;
        }
    }

    private string TokenStyle => LyoFormatterPlaceholderPalette.CssVariables(Segment.PlaceholderKey, Unresolved);

    private void OnEnter() => Session.HoveredPlaceholder = Segment.PlaceholderKey;

    private void OnLeave()
    {
        if (Session.IsHovered(Segment.PlaceholderKey))
            Session.HoveredPlaceholder = null;
    }
}
