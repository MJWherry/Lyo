using System.Diagnostics;

namespace Lyo.QRCode.Models;

/// <summary>Configuration options for QR code service implementations.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class QRCodeServiceOptions
{
    /// <summary>Configuration section name for binding from <c>IConfiguration</c>.</summary>
    public const string SectionName = "QRCodeService";

    /// <summary>Gets or sets the default <see cref="QRCodeOptions.Size" /> (pixels per module). Default: 256.</summary>
    public int DefaultSize { get; set; } = 256;

    /// <summary>Gets or sets the default error correction level. Default: Medium</summary>
    public QRCodeErrorCorrectionLevel DefaultErrorCorrectionLevel { get; set; } = QRCodeErrorCorrectionLevel.Medium;

    /// <summary>Gets or sets the default QR code format. Default: PNG</summary>
    public QRCodeFormat DefaultFormat { get; set; } = QRCodeFormat.Png;

    /// <summary>
    /// Minimum allowed <see cref="QRCodeOptions.Size" /> (pixels per module), not total image width/height. Default: 1. Hosts may raise this (e.g. 50) to block tiny raster outputs.
    /// </summary>
    public int MinSize { get; set; } = 1;

    /// <summary>Maximum allowed <see cref="QRCodeOptions.Size" /> (pixels per module). Default: 2000.</summary>
    public int MaxSize { get; set; } = 2000;

    /// <summary>Enable metrics collection for QR code operations. Default: false</summary>
    public bool EnableMetrics { get; set; } = false;

    public override string ToString()
        => $"DefaultSize: {DefaultSize}, DefaultErrorCorrectionLevel: {DefaultErrorCorrectionLevel}, DefaultFormat: {DefaultFormat}, MinSize: {MinSize}, MaxSize: {MaxSize}, EnableMetrics: {EnableMetrics}";
}