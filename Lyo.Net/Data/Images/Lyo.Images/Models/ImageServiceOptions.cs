using System.Diagnostics;
using Lyo.Exceptions;

namespace Lyo.Images.Models;

/// <summary>Settings for image service backends.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class ImageServiceOptions
{
    /// <summary>Configuration section name for IConfiguration binding. Starts as "ImageService".</summary>
    public const string SectionName = "ImageService";

    /// <summary>Default quality 1-100 for lossy formats. Starts as 90.</summary>
    public int DefaultQuality { get; set; } = 90;

    /// <summary>Max image width in pixels. Starts as 10000.</summary>
    public int MaxWidth { get; set; } = 10000;

    /// <summary>Max image height in pixels. Starts as 10000.</summary>
    public int MaxHeight { get; set; } = 10000;

    /// <summary>Max file size in bytes. Starts as 100MB.</summary>
    public long MaxFileSizeBytes { get; set; } = 100 * 1024 * 1024;

    /// <summary>If true, collect metrics for image operations. Starts as false.</summary>
    public bool EnableMetrics { get; set; } = false;

    /// <summary>If true, skip transparent and near-transparent pixels when extracting palettes. Starts as true.</summary>
    public bool IgnoreTransparentPixelsInPalette { get; set; } = true;

    /// <summary>Minimum alpha (0-255) for a pixel to count in palette extraction when <see cref="IgnoreTransparentPixelsInPalette" /> is on. Starts as 16.</summary>
    public int PaletteAlphaCutoff { get; set; } = 16;

    /// <summary>If true, QR frame compositing and center-overlay PNG use faster PNG compression (larger files, less CPU). Starts as false.</summary>
    public bool UseFastPngForQrComposites { get; set; }

    /// <summary>Throws when quality, pixel, or file-size limits are invalid.</summary>
    public void Validate()
    {
        ArgumentHelpers.ThrowIfNotInRange(DefaultQuality, 1, 100);
        ArgumentHelpers.ThrowIfNegativeOrZero(MaxWidth);
        ArgumentHelpers.ThrowIfNegativeOrZero(MaxHeight);
        ArgumentHelpers.ThrowIfNegativeOrZero(MaxFileSizeBytes);
        ArgumentHelpers.ThrowIfNotInRange(PaletteAlphaCutoff, 0, 255);
    }

    public override string ToString()
        => $"DefaultQuality: {DefaultQuality}, MaxWidth: {MaxWidth}, MaxHeight: {MaxHeight}, MaxFileSizeBytes: {MaxFileSizeBytes}, EnableMetrics: {EnableMetrics}, IgnoreTransparentPixelsInPalette: {IgnoreTransparentPixelsInPalette}, PaletteAlphaCutoff: {PaletteAlphaCutoff}, UseFastPngForQrComposites: {UseFastPngForQrComposites}";
}