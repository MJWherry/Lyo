using System.Diagnostics;

namespace Lyo.Barcode.Models;

/// <summary>A barcode generation request (e.g. for batch operations).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class BarcodeRequest
{
    /// <summary>Payload to encode.</summary>
    public string Data { get; set; } = null!;

    /// <summary>Barcode type.</summary>
    public BarcodeSymbology Symbology { get; set; } = BarcodeSymbology.Code128;

    /// <summary>Per-request raster/SVG options; null uses service defaults.</summary>
    public BarcodeOptions? Options { get; set; }

    /// <summary>Optional correlation id for batch results.</summary>
    public string? Id { get; set; }

    public override string ToString()
        => $"Id: {Id ?? "(none)"}, Symbology: {Symbology}, Data: {Data[..Math.Min(Data.Length, 50)]}{(Data.Length > 50 ? "..." : "")}, Options: {Options?.ToString() ?? "(default)"}";
}