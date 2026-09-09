using System.Diagnostics;

namespace Lyo.Barcode.Models;

/// <summary>Settings that <see cref="IBarcodeService" /> implementations bind and validate against.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class BarcodeServiceOptions
{
    /// <summary>Section key used when binding from <c>IConfiguration</c>.</summary>
    public const string SectionName = "BarcodeService";

    /// <summary>Format used when a request leaves <see cref="BarcodeOptions.Format" /> unset.</summary>
    public BarcodeFormat DefaultFormat { get; set; } = BarcodeFormat.Bmp;

    /// <summary>Module width in pixels when a request does not override it.</summary>
    public int DefaultModuleWidthPixels { get; set; } = 2;

    /// <summary>Bar height in pixels when a request does not override it.</summary>
    public int DefaultBarHeightPixels { get; set; } = 80;

    /// <summary>Quiet-zone width in modules when a request does not override it.</summary>
    public int DefaultQuietZoneModules { get; set; } = 10;

    /// <summary>Lowest module width accepted during validation.</summary>
    public int MinModuleWidthPixels { get; set; } = 1;

    /// <summary>Highest module width accepted during validation.</summary>
    public int MaxModuleWidthPixels { get; set; } = 32;

    /// <summary>Lowest bar height accepted during validation.</summary>
    public int MinBarHeightPixels { get; set; } = 8;

    /// <summary>Highest bar height accepted during validation.</summary>
    public int MaxBarHeightPixels { get; set; } = 2000;

    /// <summary>Lowest quiet-zone width in modules accepted during validation.</summary>
    public int MinQuietZoneModules { get; set; } = 0;

    /// <summary>Highest quiet-zone width in modules accepted during validation.</summary>
    public int MaxQuietZoneModules { get; set; } = 100;

    /// <summary>Lowest border width in pixels when <see cref="BarcodeOptions.ShowBorder" /> is on.</summary>
    public int MinBorderWidthPixels { get; set; } = 1;

    /// <summary>Highest border width in pixels when <see cref="BarcodeOptions.ShowBorder" /> is on.</summary>
    public int MaxBorderWidthPixels { get; set; } = 64;

    /// <summary>If true, implementations may record timing histograms.</summary>
    public bool EnableMetrics { get; set; }

    public override string ToString()
        => $"DefaultFormat: {DefaultFormat}, DefaultModuleWidthPixels: {DefaultModuleWidthPixels}, DefaultBarHeightPixels: {DefaultBarHeightPixels}, BorderClamp: {MinBorderWidthPixels}-{MaxBorderWidthPixels}px, EnableMetrics: {EnableMetrics}";
}