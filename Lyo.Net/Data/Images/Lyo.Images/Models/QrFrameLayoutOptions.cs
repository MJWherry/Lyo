namespace Lyo.Images.Models;

/// <summary>Layout and styling for <see cref="IImageService.CompositeQrFramePngAsync" />.</summary>
public sealed class QrFrameLayoutOptions
{
    /// <summary>Frame layout. Default: <see cref="QrFrameStyle.None" />.</summary>
    public QrFrameStyle Style { get; set; } = QrFrameStyle.None;

    /// <summary>Text in the header band (<see cref="QrFrameStyle.BadgeWithHeader" />) or below the QR (<see cref="QrFrameStyle.BorderOnly" />).</summary>
    public string? CaptionText { get; set; }

    /// <summary>Header background color (hex, e.g. <c>#1e293b</c>).</summary>
    public string HeaderBackgroundHex { get; set; } = "#1e293b";

    /// <summary>Caption color in the header (hex).</summary>
    public string HeaderCaptionTextHex { get; set; } = "#FFFFFF";

    /// <summary>Main panel fill behind the QR (hex).</summary>
    public string PanelBackgroundHex { get; set; } = "#FFFFFF";

    /// <summary>Outer page/canvas behind the card (hex). Default is light gray so white panels remain visible on white web backgrounds. Use <c>#00FFFFFF</c> for transparent.</summary>
    public string CanvasBackgroundHex { get; set; } = "#FFF3F4F6";

    /// <summary>Drop shadow fill (typically semi-transparent dark, e.g. <c>#40000000</c>).</summary>
    public string ShadowHex { get; set; } = "#59000000";

    /// <summary>Padding between the QR and the inner panel edges.</summary>
    public int PaddingAroundQrPx { get; set; } = 24;

    /// <summary>Outer margin from canvas edge to the card.</summary>
    public int OuterMarginPx { get; set; } = 20;

    /// <summary>Corner radius for card and panels.</summary>
    public int CornerRadiusPx { get; set; } = 18;

    /// <summary>Height of the header band for <see cref="QrFrameStyle.BadgeWithHeader" />.</summary>
    public int HeaderHeightPx { get; set; } = 52;

    /// <summary>Caption font size in pixels.</summary>
    public int CaptionFontSizePx { get; set; } = 18;

    /// <summary>Preferred font family name (OS-dependent; falls back if missing).</summary>
    public string FontFamily { get; set; } = "DejaVu Sans";

    /// <summary>Shadow offset to the right and down.</summary>
    public int ShadowOffsetPx { get; set; } = 6;

    /// <summary>Draw a small downward tab at the bottom center of the header (badge style).</summary>
    public bool DrawHeaderNotch { get; set; } = true;

    /// <summary>Full width of the notch at the header bottom edge.</summary>
    public int NotchWidthPx { get; set; } = 36;

    /// <summary>How far the notch extends below the header baseline.</summary>
    public int NotchDepthPx { get; set; } = 10;

    /// <summary>Stroke width (pixels) around the card in <see cref="QrFrameStyle.BadgeWithHeader" /> and <see cref="QrFrameStyle.SimpleRoundedPanel" />.</summary>
    public int CardOutlineWidthPx { get; set; } = 4;

    /// <summary>Stroke color for <see cref="CardOutlineWidthPx" /> (hex).</summary>
    public string CardOutlineHex { get; set; } = "#64748B";

    /// <summary>Border stroke width for <see cref="QrFrameStyle.BorderOnly" />.</summary>
    public int BorderStrokeWidthPx { get; set; } = 10;

    /// <summary>Border stroke color (hex).</summary>
    public string BorderStrokeHex { get; set; } = "#0F172A";

    /// <summary>Extra space below the QR reserved for caption in <see cref="QrFrameStyle.BorderOnly" />.</summary>
    public int CaptionFooterPaddingPx { get; set; } = 12;
}
