using System.Diagnostics;

namespace Lyo.QRCode.Models;

/// <summary>Options for embedding an icon/logo in the center of a QR code.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class QRCodeIconOptions
{
    /// <summary>Maximum <see cref="IconSizePercent" /> for reliable scanning. Center logos above this erase too many modules even at high ECC.</summary>
    public const int MaxIconSizePercent = 30;

    /// <summary>Gets or sets the icon image bytes (optional if <see cref="IconFilePath" /> is set).</summary>
    public byte[]? IconBytes { get; set; }

    /// <summary>Gets or sets a path to an icon image file (optional if <see cref="IconBytes" /> is set).</summary>
    public string? IconFilePath { get; set; }

    /// <summary>Gets or sets the icon width/height as a percentage of the QR image side (1–<see cref="MaxIconSizePercent" />). Default: 15</summary>
    public int IconSizePercent { get; set; } = 15;

    /// <summary>Clamps <paramref name="iconSizePercent" /> to <c>1</c>…<see cref="MaxIconSizePercent" /> (e.g. for compositing when options were set without validation).</summary>
    public static int ClampIconSizePercent(int iconSizePercent) => Math.Clamp(iconSizePercent, 1, MaxIconSizePercent);

    /// <summary>Gets or sets whether to draw a border around the icon. Default: true</summary>
    public bool DrawIconBorder { get; set; } = true;

    public override string ToString()
        => $"IconSizePercent: {IconSizePercent}, DrawIconBorder: {DrawIconBorder}, IconBytesLength: {IconBytes?.Length ?? 0}, IconFilePath: {IconFilePath}";
}