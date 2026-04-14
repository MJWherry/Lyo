using System.Diagnostics;

namespace Lyo.QRCode.Models;

/// <summary>Configuration options for QR code service implementations.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class QRCodeServiceOptions
{
    public const string SectionName = "QRCodeService";

    /// <summary>Gets or sets the default QR code size in pixels. Default: 256</summary>
    public int DefaultSize { get; set; } = 256;

    /// <summary>Gets or sets the default error correction level. Default: Medium</summary>
    public QRCodeErrorCorrectionLevel DefaultErrorCorrectionLevel { get; set; } = QRCodeErrorCorrectionLevel.Medium;

    /// <summary>Gets or sets the default QR code format. Default: PNG</summary>
    public QRCodeFormat DefaultFormat { get; set; } = QRCodeFormat.Png;

    /// <summary>Gets or sets the minimum QR code size in pixels. Default: 50</summary>
    public int MinSize { get; set; } = 50;

    /// <summary>Gets or sets the maximum QR code size in pixels. Default: 2000</summary>
    public int MaxSize { get; set; } = 2000;

    /// <summary>Enable metrics collection for QR code operations. Default: false</summary>
    public bool EnableMetrics { get; set; } = false;

    public override string ToString()
        => $"DefaultSize: {DefaultSize}, DefaultErrorCorrectionLevel: {DefaultErrorCorrectionLevel}, DefaultFormat: {DefaultFormat}, MinSize: {MinSize}, MaxSize: {MaxSize}, EnableMetrics: {EnableMetrics}";
}