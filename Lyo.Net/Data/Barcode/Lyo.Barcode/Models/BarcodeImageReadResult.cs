namespace Lyo.Barcode.Models;

/// <summary>Payload from decoding a barcode image (e.g. ZXing).</summary>
public sealed record BarcodeImageReadResult
{
    public string Text { get; init; } = "";

    /// <summary>Decoder format name (e.g. <c>CODE_128</c>).</summary>
    public string FormatName { get; init; } = "";
}