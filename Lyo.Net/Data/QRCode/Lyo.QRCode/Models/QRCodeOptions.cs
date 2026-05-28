using System.Diagnostics;

namespace Lyo.QRCode.Models;

/// <summary>
/// Configuration options for QR code generation. Decoration (frame, caption, padding) is intentionally out of scope; chain <c>Lyo.Images.IImageDecorationService</c> on the
/// returned bytes for that.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public class QRCodeOptions
{
    /// <summary>Gets or sets the QR code format (PNG, SVG, etc.). Default: PNG</summary>
    public QRCodeFormat Format { get; set; } = QRCodeFormat.Png;

    /// <summary>
    /// Gets or sets the pixel size of each module (square “dot”) in the rendered image—not the total image width/height. Default: 256. Total output size is this value times the
    /// number of modules on a side (including quiet zone when drawn).
    /// </summary>
    public int Size { get; set; } = 256;

    /// <summary>
    /// Gets or sets the requested error correction level. Default: Medium. When <see cref="Icon" /> is set, the encoder may use a higher level so larger center logos remain
    /// scannable (this is the only effect of <see cref="Icon" /> on the encoder output—actual icon compositing is the consumer's responsibility).
    /// </summary>
    public QRCodeErrorCorrectionLevel ErrorCorrectionLevel { get; set; } = QRCodeErrorCorrectionLevel.Medium;

    /// <summary>Gets or sets the dark color (foreground) in hex format (e.g., "#000000"). Default: "#000000"</summary>
    public string DarkColor { get; set; } = "#000000";

    /// <summary>Gets or sets the light color (background) in hex format (e.g., "#FFFFFF"). Default: "#FFFFFF"</summary>
    public string LightColor { get; set; } = "#FFFFFF";

    /// <summary>Gets or sets whether to draw a quiet zone (white border around QR code). Default: true</summary>
    public bool DrawQuietZones { get; set; } = true;

    /// <summary>
    /// Hint that the consumer plans to overlay a center logo. Used only for ECC level selection — set <see cref="QRCodeIconOptions.IconSizePercent" /> so the encoder bumps ECC
    /// high enough for the logo to remain scannable. The encoder never reads <see cref="QRCodeIconOptions.IconBytes" /> / <see cref="QRCodeIconOptions.IconFilePath" />; compose the icon
    /// with <c>IImageDecorationService.OverlayAsync</c> on the returned bytes.
    /// </summary>
    public QRCodeIconOptions? Icon { get; set; }

    public override string ToString()
        => $"Format: {Format}, Size: {Size}, ErrorCorrectionLevel: {ErrorCorrectionLevel}, DarkColor: {DarkColor}, LightColor: {LightColor}, DrawQuietZones: {DrawQuietZones}, HasIcon: {Icon != null}";
}