using System.Diagnostics;

namespace Lyo.Barcode.Models;

/// <summary>One barcode to render, typically one item in a batch.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class BarcodeRequest
{
    /// <summary>Text to encode.</summary>
    public string Data { get; set; } = null!;

    /// <summary>Which barcode family to draw.</summary>
    public BarcodeSymbology Symbology { get; set; } = BarcodeSymbology.Code128;

    /// <summary>Raster or SVG settings for this item. Null falls back to service defaults.</summary>
    public BarcodeOptions? Options { get; set; }

    /// <summary>Optional id used to match this item in batch results.</summary>
    public string? Id { get; set; }

    public override string ToString()
        => $"Id: {Id ?? "(none)"}, Symbology: {Symbology}, Data: {Data[..Math.Min(Data.Length, 50)]}{(Data.Length > 50 ? "..." : "")}, Options: {Options?.ToString() ?? "(default)"}";
}