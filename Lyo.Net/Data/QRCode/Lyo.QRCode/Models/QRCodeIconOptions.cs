using System.Diagnostics;

namespace Lyo.QRCode.Models;

/// <summary>
/// Hints for the QR encoder about a planned center logo. The encoder uses <see cref="IconSizePercent" /> to choose an ECC level high enough that the logo can erase modules
/// without breaking scanning; <see cref="IconBytes" />, <see cref="IconFilePath" />, and <see cref="DrawIconBorder" /> are metadata for the consumer's overlay call (e.g.
/// <c>IImageDecorationService.OverlayAsync</c> in <c>Lyo.Images</c>) and are never read by the encoder itself.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public class QRCodeIconOptions
{
    /// <summary>Maximum <see cref="IconSizePercent" /> for reliable scanning. Center logos above this erase too many modules even at high ECC.</summary>
    public const int MaxIconSizePercent = 30;

    /// <summary>Icon image bytes carried alongside the QR options for the consumer's overlay step (not read by the encoder).</summary>
    public byte[]? IconBytes { get; set; }

    /// <summary>Path to an icon image file carried alongside the QR options for the consumer's overlay step (not read by the encoder).</summary>
    public string? IconFilePath { get; set; }

    /// <summary>
    /// Planned icon width/height as a percent of the QR side (1–<see cref="MaxIconSizePercent" />). Default: 15. This is the only field the encoder uses—to pick an ECC level
    /// robust to that erased fraction.
    /// </summary>
    public int IconSizePercent { get; set; } = 15;

    /// <summary>Whether the consumer's overlay step should draw a border around the icon. Metadata only; never read by the encoder.</summary>
    public bool DrawIconBorder { get; set; } = true;

    /// <summary>Clamps <paramref name="iconSizePercent" /> to <c>1</c>…<see cref="MaxIconSizePercent" /> (e.g. for compositing when options were set without validation).</summary>
    public static int ClampIconSizePercent(int iconSizePercent) => Math.Clamp(iconSizePercent, 1, MaxIconSizePercent);

    public override string ToString()
        => $"IconSizePercent: {IconSizePercent}, DrawIconBorder: {DrawIconBorder}, IconBytesLength: {IconBytes?.Length ?? 0}, IconFilePath: {IconFilePath}";
}