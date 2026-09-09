using System.Diagnostics;

namespace Lyo.QRCode.Models;

/// <summary>
/// Hints for the QR encoder about a planned center logo. The encoder uses <see cref="IconSizePercent" /> to pick an ECC level high enough that the logo can erase modules
/// without breaking scanning. <see cref="IconBytes" />, <see cref="IconFilePath" />, and <see cref="DrawIconBorder" /> are metadata for the consumer's overlay call (for example
/// <c>IImageDecorationService.OverlayAsync</c> in <c>Lyo.Images</c>) and are never read by the encoder itself.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public class QRCodeIconOptions
{
    /// <summary>Ceiling for <see cref="IconSizePercent" /> if scanning should stay reliable. Logos above this erase too many modules even at high ECC.</summary>
    public const int MaxIconSizePercent = 30;

    /// <summary>Icon image bytes stored with the QR options for the consumer's overlay step. The encoder does not read this.</summary>
    public byte[]? IconBytes { get; set; }

    /// <summary>Path to an icon image file stored with the QR options for the consumer's overlay step. The encoder does not read this.</summary>
    public string? IconFilePath { get; set; }

    /// <summary>
    /// Planned icon width and height as a percent of the QR side (1–<see cref="MaxIconSizePercent" />). Starts as 15. This is the only field the encoder uses: it picks an ECC
    /// level that can survive that erased fraction.
    /// </summary>
    public int IconSizePercent { get; set; } = 15;

    /// <summary>If true, the consumer's overlay step should draw a border around the icon. Metadata only. The encoder never reads this.</summary>
    public bool DrawIconBorder { get; set; } = true;

    /// <summary>
    /// Clamps <paramref name="iconSizePercent" /> to <c>1</c>…<see cref="MaxIconSizePercent" /> (for compositing when options were set without validation, for example).
    /// </summary>
    public static int ClampIconSizePercent(int iconSizePercent) => Math.Clamp(iconSizePercent, 1, MaxIconSizePercent);

    public override string ToString()
        => $"IconSizePercent: {IconSizePercent}, DrawIconBorder: {DrawIconBorder}, IconBytesLength: {IconBytes?.Length ?? 0}, IconFilePath: {IconFilePath}";
}