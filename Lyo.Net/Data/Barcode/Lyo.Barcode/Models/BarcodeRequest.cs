using System.Diagnostics;

namespace Lyo.Barcode.Models;

/// <summary>A barcode generation request (e.g. for batch operations).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class BarcodeRequest
{
    public string Data { get; set; } = null!;

    public BarcodeSymbology Symbology { get; set; } = BarcodeSymbology.Code128;

    public BarcodeOptions? Options { get; set; }

    public string? Id { get; set; }

    public override string ToString()
        => $"Id: {Id ?? "(none)"}, Symbology: {Symbology}, Data: {Data?.Substring(0, Math.Min(Data?.Length ?? 0, 50))}{(Data?.Length > 50 ? "..." : "")}, Options: {Options?.ToString() ?? "(default)"}";
}