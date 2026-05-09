using System.Diagnostics;

namespace Lyo.Barcode.Models;

/// <summary>Payload from decoding a barcode image (e.g. ZXing).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record BarcodeImageReadResult
{
    /// <summary>Decoded payload string.</summary>
    public string Text { get; init; } = "";

    /// <summary>Decoder format name (e.g. <c>CODE_128</c>).</summary>
    public string FormatName { get; init; } = "";

    /// <inheritdoc />
    public override string ToString()
    {
        var p = Text.Length <= 48 ? Text : Text[..48] + "…";
        return $"{FormatName}: \"{p}\"";
    }
}