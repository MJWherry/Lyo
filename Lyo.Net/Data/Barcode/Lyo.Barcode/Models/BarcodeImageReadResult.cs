using System.Diagnostics;

namespace Lyo.Barcode.Models;

/// <summary>Text and format name returned after decoding a barcode image (ZXing and similar).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record BarcodeImageReadResult
{
    /// <summary>Text recovered from the symbol.</summary>
    public string Text { get; init; } = "";

    /// <summary>Format name reported by the decoder, such as <c>CODE_128</c>.</summary>
    public string FormatName { get; init; } = "";

    /// <inheritdoc />
    public override string ToString()
    {
        var p = Text.Length <= 48 ? Text : Text[..48] + "…";
        return $"{FormatName}: \"{p}\"";
    }
}