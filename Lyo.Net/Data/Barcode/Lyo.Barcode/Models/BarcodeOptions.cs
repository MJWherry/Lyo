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

    public override string ToString()
        => $"Format: {Format}, ModuleWidthPixels: {ModuleWidthPixels}, BarHeightPixels: {BarHeightPixels}, QuietZoneModules: {QuietZoneModules}, DarkColor: {DarkColor}, LightColor: {LightColor}";
}