using System.Diagnostics;

namespace Lyo.QRCode.Models;

/// <summary>
/// Settings for QR generation. Decoration (frame, caption, padding) is out of scope on purpose. Chain <c>Lyo.Images.IImageDecorationService</c> on the
/// returned bytes for that.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public class QRCodeOptions
{
    /// <summary>QR format (PNG, SVG, and similar). Starts as PNG.</summary>
    public QRCodeFormat Format { get; set; } = QRCodeFormat.Png;

    /// <summary>
    /// Pixel size of each module (square “dot”) in the rendered image, not the total image width or height. Starts as 256. Total output size is this value times the
    /// number of modules on a side (including the quiet zone when drawn).
    /// </summary>
    public int Size { get; set; } = 256;

    /// <summary>
    /// Requested error-correction level. Starts as Medium. When <see cref="Icon" /> is set, the encoder may use a higher level so larger center logos stay
    /// scannable. That is the only effect of <see cref="Icon" /> on encoder output. Actual icon compositing is the consumer's job.
    /// </summary>
    public QRCodeErrorCorrectionLevel ErrorCorrectionLevel { get; set; } = QRCodeErrorCorrectionLevel.Medium;

    /// <summary>Dark (foreground) color in hex (for example <c>#000000</c>). Starts as <c>#000000</c>.</summary>
    public string DarkColor { get; set; } = "#000000";

    /// <summary>Light (background) color in hex (for example <c>#FFFFFF</c>). Starts as <c>#FFFFFF</c>.</summary>
    public string LightColor { get; set; } = "#FFFFFF";

    /// <summary>If true, draw a quiet zone (white border around the QR code). Starts as true.</summary>
    public bool DrawQuietZones { get; set; } = true;

    /// <summary>
    /// Hint that the consumer plans to overlay a center logo. Used only for ECC level selection. Set <see cref="QRCodeIconOptions.IconSizePercent" /> so the encoder bumps ECC
    /// high enough for the logo to stay scannable. The encoder never reads <see cref="QRCodeIconOptions.IconBytes" /> /
    /// <see cref="QRCodeIconOptions.IconFilePath" />; compose the icon with <c>IImageDecorationService.OverlayAsync</c> on the returned bytes.
    /// </summary>
    public QRCodeIconOptions? Icon { get; set; }

    public override string ToString()
        => $"Format: {Format}, Size: {Size}, ErrorCorrectionLevel: {ErrorCorrectionLevel}, DarkColor: {DarkColor}, LightColor: {LightColor}, DrawQuietZones: {DrawQuietZones}, HasIcon: {Icon != null}";
}