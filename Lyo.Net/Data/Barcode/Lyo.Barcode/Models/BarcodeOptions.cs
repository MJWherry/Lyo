using System.Diagnostics;

namespace Lyo.Barcode.Models;

/// <summary>Per-request settings that control how a barcode image is drawn.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class BarcodeOptions
{
    /// <summary>Image type to emit. Starts as <see cref="BarcodeFormat.Bmp" />.</summary>
    public BarcodeFormat Format { get; set; } = BarcodeFormat.Bmp;

    /// <summary>Pixel width of one module (horizontal pitch). Starts at 2.</summary>
    public int ModuleWidthPixels { get; set; } = 2;

    /// <summary>Pixel height of the bars, not counting quiet zone. Starts at 80.</summary>
    public int BarHeightPixels { get; set; } = 80;

    /// <summary>Quiet-zone width on each side, in modules. Starts at 10.</summary>
    public int QuietZoneModules { get; set; } = 10;

    /// <summary>Bar (foreground) color as hex, for example <c>#000000</c>.</summary>
    public string DarkColor { get; set; } = "#000000";

    /// <summary>Background color as hex, for example <c>#FFFFFF</c>.</summary>
    public string LightColor { get; set; } = "#FFFFFF";

    /// <summary>If true, centered HRI text is drawn under the bars. BMP uses ImageSharp text layout; SVG uses a <c>&lt;text&gt;</c> element.</summary>
    public bool ShowHumanReadableTextBelow { get; set; }

    /// <summary>
    /// Caption under the bars when <see cref="ShowHumanReadableTextBelow" /> is on. Null or whitespace falls back to the encoded payload, the same
    /// string given to <see cref="IBarcodeService.GenerateAsync(string, BarcodeSymbology, BarcodeOptions?, CancellationToken)" />.
    /// </summary>
    public string? HumanReadableText { get; set; }

    /// <summary>Caption font size in pixels. Starts at 14.</summary>
    public int HumanReadableFontSizePixels { get; set; } = 14;

    /// <summary>Pixels between the bottom of the bar band and the caption baseline area. Starts at 6.</summary>
    public int HumanReadableMarginTopPixels { get; set; } = 6;

    /// <summary>Extra pixels under the caption. Starts at 4.</summary>
    public int HumanReadableMarginBottomPixels { get; set; } = 4;

    /// <summary>Caption ink color as hex. Null or empty uses <see cref="DarkColor" />.</summary>
    public string? HumanReadableColorHex { get; set; }

    /// <summary>If true, a filled frame is drawn outside the symbol quiet zone. <see cref="Native.NativeBarcodeService" /> implements this.</summary>
    public bool ShowBorder { get; set; }

    /// <summary>Thickness in pixels of each side of the frame when <see cref="ShowBorder" /> is on. Starts at 2.</summary>
    public int BorderWidthPixels { get; set; } = 2;

    /// <summary>Frame color as hex (for example <c>#000000</c>). Applied only when <see cref="ShowBorder" /> is on.</summary>
    public string BorderColorHex { get; set; } = "#000000";

    public override string ToString()
        => $"Format: {Format}, ModuleWidthPixels: {ModuleWidthPixels}, BarHeightPixels: {BarHeightPixels}, QuietZoneModules: {QuietZoneModules}, DarkColor: {DarkColor}, LightColor: {LightColor}, ShowHumanReadableTextBelow: {ShowHumanReadableTextBelow}, ShowBorder: {ShowBorder}";
}