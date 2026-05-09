using System.Diagnostics;

namespace Lyo.QRCode.Models;

/// <summary>Payload from decoding a QR code image (e.g. ZXing).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record QRCodeImageReadResult
{
    public string Text { get; init; } = "";

    /// <summary>Decoder format name (e.g. <c>QR_CODE</c>).</summary>
    public string FormatName { get; init; } = "";

    /// <inheritdoc />
    public override string ToString()
    {
        var p = Text.Length <= 48 ? Text : Text[..48] + "…";
        return $"{FormatName}: \"{p}\"";
    }
}