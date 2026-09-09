using System.Diagnostics;

namespace Lyo.QRCode.Models;

/// <summary>Payload from decoding a QR image (ZXing, for example).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record QRCodeImageReadResult
{
    public string Text { get; init; } = "";

    /// <summary>Decoder format name (for example <c>QR_CODE</c>).</summary>
    public string FormatName { get; init; } = "";

    /// <inheritdoc />
    public override string ToString()
    {
        var p = Text.Length <= 48 ? Text : Text[..48] + "…";
        return $"{FormatName}: \"{p}\"";
    }
}