using System.Diagnostics;

namespace Lyo.Barcode.Models;

/// <summary>Configuration for <see cref="IBarcodeService" /> implementations.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class BarcodeServiceOptions
{
    /// <summary>Configuration section name for binding from <c>IConfiguration</c>.</summary>
    public const string SectionName = "BarcodeService";

    /// <summary>Default output format when requests omit <see cref="BarcodeOptions.Format" />.</summary>
    public BarcodeFormat DefaultFormat { get; set; } = BarcodeFormat.Bmp;

    /// <summary>Default module width in pixels.</summary>
    public int DefaultModuleWidthPixels { get; set; } = 2;

    /// <summary>Default bar height in pixels.</summary>
    public int DefaultBarHeightPixels { get; set; } = 80;

    /// <summary>Default quiet zone width in modules.</summary>
    public int DefaultQuietZoneModules { get; set; } = 10;

    /// <summary>Minimum allowed module width (validation).</summary>
    public int MinModuleWidthPixels { get; set; } = 1;

    /// <summary>Maximum allowed module width (validation).</summary>
    public int MaxModuleWidthPixels { get; set; } = 32;

    /// <summary>Minimum allowed bar height (validation).</summary>
    public int MinBarHeightPixels { get; set; } = 8;

    /// <summary>Maximum allowed bar height (validation).</summary>
    public int MaxBarHeightPixels { get; set; } = 2000;

    /// <summary>Minimum quiet zone modules (validation).</summary>
    public int MinQuietZoneModules { get; set; } = 0;

    /// <summary>Maximum quiet zone modules (validation).</summary>
    public int MaxQuietZoneModules { get; set; } = 100;

    /// <summary>Minimum border width in pixels when <see cref="BarcodeOptions.ShowBorder" /> is true.</summary>
    public int MinBorderWidthPixels { get; set; } = 1;

    /// <summary>Maximum border width in pixels when <see cref="BarcodeOptions.ShowBorder" /> is true.</summary>
    public int MaxBorderWidthPixels { get; set; } = 64;

    /// <summary>When true, implementations may emit timing histograms.</summary>
    public bool EnableMetrics { get; set; }

    public override string ToString()
        => $"DefaultFormat: {DefaultFormat}, DefaultModuleWidthPixels: {DefaultModuleWidthPixels}, DefaultBarHeightPixels: {DefaultBarHeightPixels}, BorderClamp: {MinBorderWidthPixels}-{MaxBorderWidthPixels}px, EnableMetrics: {EnableMetrics}";
}