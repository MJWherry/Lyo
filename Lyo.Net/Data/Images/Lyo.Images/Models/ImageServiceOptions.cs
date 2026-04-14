using System.Diagnostics;

namespace Lyo.Images.Models;

/// <summary>Configuration options for image service implementations.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class ImageServiceOptions
{
    /// <summary>Configuration section name for binding from IConfiguration. Default: "ImageService"</summary>
    public const string SectionName = "ImageService";

    /// <summary>Gets or sets the default image quality (1-100, for lossy formats). Default: 90</summary>
    public int DefaultQuality { get; set; } = 90;

    /// <summary>Gets or sets the maximum image width in pixels. Default: 10000</summary>
    public int MaxWidth { get; set; } = 10000;

    /// <summary>Gets or sets the maximum image height in pixels. Default: 10000</summary>
    public int MaxHeight { get; set; } = 10000;

    /// <summary>Gets or sets the maximum file size in bytes. Default: 100MB</summary>
    public long MaxFileSizeBytes { get; set; } = 100 * 1024 * 1024;

    /// <summary>Enable metrics collection for image operations. Default: false</summary>
    public bool EnableMetrics { get; set; } = false;

    /// <summary>Ignore transparent and near-transparent pixels when extracting image palettes. Default: true</summary>
    public bool IgnoreTransparentPixelsInPalette { get; set; } = true;

    /// <summary>Minimum alpha (0-255) for a pixel to be counted during palette extraction when <see cref="IgnoreTransparentPixelsInPalette" /> is enabled. Default: 16.</summary>
    public int PaletteAlphaCutoff { get; set; } = 16;

    public override string ToString()
        => $"DefaultQuality: {DefaultQuality}, MaxWidth: {MaxWidth}, MaxHeight: {MaxHeight}, MaxFileSizeBytes: {MaxFileSizeBytes}, EnableMetrics: {EnableMetrics}, IgnoreTransparentPixelsInPalette: {IgnoreTransparentPixelsInPalette}, PaletteAlphaCutoff: {PaletteAlphaCutoff}";
}