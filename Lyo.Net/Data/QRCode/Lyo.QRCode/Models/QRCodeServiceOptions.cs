using System.Diagnostics;
using Lyo.Exceptions;

namespace Lyo.QRCode.Models;

/// <summary>Settings for QR service implementations.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class QRCodeServiceOptions
{
    /// <summary>Section key used when binding from <c>IConfiguration</c>.</summary>
    public const string SectionName = "QRCodeService";

    /// <summary>Default <see cref="QRCodeOptions.Size" /> (pixels per module). Starts as 256.</summary>
    public int DefaultSize { get; set; } = 256;

    /// <summary>Default error-correction level. Starts as Medium.</summary>
    public QRCodeErrorCorrectionLevel DefaultErrorCorrectionLevel { get; set; } = QRCodeErrorCorrectionLevel.Medium;

    /// <summary>Default QR format. Starts as PNG.</summary>
    public QRCodeFormat DefaultFormat { get; set; } = QRCodeFormat.Png;

    /// <summary>
    /// Minimum allowed <see cref="QRCodeOptions.Size" /> (pixels per module), not total image width or height. Starts as 1. Hosts may raise this (for example 50) to block tiny
    /// raster outputs.
    /// </summary>
    public int MinSize { get; set; } = 1;

    /// <summary>Maximum allowed <see cref="QRCodeOptions.Size" /> (pixels per module). Starts as 2000.</summary>
    public int MaxSize { get; set; } = 2000;

    /// <summary>If true, collect metrics for QR operations. Starts as false.</summary>
    public bool EnableMetrics { get; set; } = false;

    /// <summary>Throws when size bounds or default format/level are invalid.</summary>
    public void Validate()
    {
        ArgumentHelpers.ThrowIfNotDefined(DefaultErrorCorrectionLevel);
        ArgumentHelpers.ThrowIfNotDefined(DefaultFormat);
        ArgumentHelpers.ThrowIfNegativeOrZero(MinSize);
        ArgumentHelpers.ThrowIfLessThan(MaxSize, MinSize);
        ArgumentHelpers.ThrowIfNotInRange(DefaultSize, MinSize, MaxSize);
    }

    public override string ToString()
        => $"DefaultSize: {DefaultSize}, DefaultErrorCorrectionLevel: {DefaultErrorCorrectionLevel}, DefaultFormat: {DefaultFormat}, MinSize: {MinSize}, MaxSize: {MaxSize}, EnableMetrics: {EnableMetrics}";
}