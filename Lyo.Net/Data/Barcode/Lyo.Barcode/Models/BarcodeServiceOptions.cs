using System.Diagnostics;

namespace Lyo.Barcode.Models;

/// <summary>Configuration for <see cref="IBarcodeService" /> implementations.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class BarcodeServiceOptions
{
    public const string SectionName = "BarcodeService";

    public BarcodeFormat DefaultFormat { get; set; } = BarcodeFormat.Bmp;

    public int DefaultModuleWidthPixels { get; set; } = 2;

    public int DefaultBarHeightPixels { get; set; } = 80;

    public int DefaultQuietZoneModules { get; set; } = 10;

    public int MinModuleWidthPixels { get; set; } = 1;

    public int MaxModuleWidthPixels { get; set; } = 32;

    public int MinBarHeightPixels { get; set; } = 8;

    public int MaxBarHeightPixels { get; set; } = 2000;

    public int MinQuietZoneModules { get; set; } = 0;

    public int MaxQuietZoneModules { get; set; } = 100;

    public bool EnableMetrics { get; set; }

    public override string ToString()
        => $"DefaultFormat: {DefaultFormat}, DefaultModuleWidthPixels: {DefaultModuleWidthPixels}, DefaultBarHeightPixels: {DefaultBarHeightPixels}, EnableMetrics: {EnableMetrics}";
}