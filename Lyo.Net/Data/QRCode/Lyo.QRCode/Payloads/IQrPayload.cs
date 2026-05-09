namespace Lyo.QRCode.Payloads;

/// <summary>Typed content that serializes to the string encoded in a QR symbol (URLs, <c>WIFI:</c>, vCard, etc.).</summary>
public interface IQrPayload
{
    /// <summary>Returns the exact string passed to QR encoders (for example the <c>data</c> argument to QR generation APIs).</summary>
    string ToQrString();
}
