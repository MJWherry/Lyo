namespace Lyo.QRCode.Payloads;

/// <summary>Typed content that serializes to the string encoded in a QR symbol (URLs, <c>WIFI:</c>, vCard, and similar).</summary>
public interface IQrPayload
{
    /// <summary>Exact string passed to QR encoders (the <c>data</c> argument to generation APIs, for example).</summary>
    string ToQrString();
}