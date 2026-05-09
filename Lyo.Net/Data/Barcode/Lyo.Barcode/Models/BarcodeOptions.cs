using System.Diagnostics;

namespace Lyo.Barcode.Models;

/// <summary>Options for barcode image generation.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class BarcodeOptions
{
    /// <summary>Output format. Default: <see cref="BarcodeFormat.Bmp" />.</summary>
    public BarcodeFormat Format { get; set; } = BarcodeFormat.Bmp;

    /// <summary>Width of one bar module in pixels (horizontal pitch). Default: 2.</summary>
    public int ModuleWidthPixels { get; set; } = 2;

    /// <summary>Height of bars in pixels (excluding quiet zone). Default: 80.</summary>
    public int BarHeightPixels { get; set; } = 80;

    /// <summary>Quiet zone width on each side, in modules. Default: 10.</summary>
    public int QuietZoneModules { get; set; } = 10;

    /// <summary>Foreground (bars) color, hex (e.g. <c>#000000</c>).</summary>
    public string DarkColor { get; set; } = "#000000";

    /// <summary>Background color, hex (e.g. <c>#FFFFFF</c>).</summary>
    public string LightColor { get; set; } = "#FFFFFF";

    /// <summary>When true, draws human-readable text centered under the bars (HRI). BMP uses ImageSharp text layout; SVG uses a <c>&lt;text&gt;</c> element.</summary>
    public bool ShowHumanReadableTextBelow { get; set; }

    /// <summary>
    /// Optional caption under the bars when <see cref="ShowHumanReadableTextBelow" /> is true. Null or whitespace uses the encoded payload (e.g. the same string passed to
    /// <see cref="IBarcodeService.GenerateAsync(string, BarcodeSymbology, BarcodeOptions?, CancellationToken)" />).
    /// </summary>
    public string? HumanReadableText { get; set; }

    /// <summary>Font size in pixels for the caption under the bars. Default: 14.</summary>
    public int HumanReadableFontSizePixels { get; set; } = 14;

    /// <summary>Vertical gap in pixels between the bottom of the bar band and the caption baseline area. Default: 6.</summary>
    public int HumanReadableMarginTopPixels { get; set; } = 6;

    /// <summary>Extra padding below the caption text in pixels. Default: 4.</summary>
    public int HumanReadableMarginBottomPixels { get; set; } = 4;

    /// <summary>Caption ink color, hex. Null or empty uses <see cref="DarkColor" />.</summary>
    public string? HumanReadableColorHex { get; set; }

    /// <summary>When true, draws a filled frame around the barcode (outside symbol quiet zone). Implemented by <see cref="Native.NativeBarcodeService" />.</summary>
    public bool ShowBorder { get; set; }

    /// <summary>Border thickness in pixels on each side when <see cref="ShowBorder" /> is true. Default: 2.</summary>
    public int BorderWidthPixels { get; set; } = 2;

    /// <summary>Border color as hex (e.g. <c>#000000</c>). Used only when <see cref="ShowBorder" /> is true.</summary>
    public string BorderColorHex { get; set; } = "#000000";

    public override string ToString()
        => $"Format: {Format}, ModuleWidthPixels: {ModuleWidthPixels}, BarHeightPixels: {BarHeightPixels}, QuietZoneModules: {QuietZoneModules}, DarkColor: {DarkColor}, LightColor: {LightColor}, ShowHumanReadableTextBelow: {ShowHumanReadableTextBelow}, ShowBorder: {ShowBorder}";
}