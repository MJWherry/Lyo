namespace Lyo.QRCode.Models;

/// <summary>Payload from decoding a QR code image (e.g. ZXing).</summary>
public sealed record QRCodeImageReadResult
{
    public string Text { get; init; } = "";

    /// <summary>Decoder format name (e.g. <c>QR_CODE</c>).</summary>
    public string FormatName { get; init; } = "";
}