namespace Lyo.QRCode.QRCoder;

/// <summary>Consolidated constants for the QRCoder QR code library.</summary>
public static class Constants
{
    /// <summary>Constants for QRCoder QR code service metrics.</summary>
    public static class Metrics
    {
        public const string GenerateDuration = "qrcode.qrcoder.generate.duration";
        public const string BatchGenerateDuration = "qrcode.qrcoder.batch.generate.duration";
        public const string GenerateSuccess = "qrcode.qrcoder.generate.success";
        public const string GenerateFailure = "qrcode.qrcoder.generate.failure";
        public const string GenerateCancelled = "qrcode.qrcoder.generate.cancelled";
    }
}