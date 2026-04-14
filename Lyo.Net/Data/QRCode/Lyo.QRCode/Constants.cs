namespace Lyo.QRCode;

/// <summary>Consolidated constants for the QRCode library.</summary>
public static class Constants
{
    /// <summary>Constants for QR code service metrics.</summary>
    public static class Metrics
    {
        /// <summary>Duration metric for QR code generation operations.</summary>
        public const string GenerateDuration = "qrcode.generate.duration";

        /// <summary>Duration metric for batch QR code generation operations.</summary>
        public const string BatchGenerateDuration = "qrcode.batch.generate.duration";

        /// <summary>Counter metric for successful QR code generations.</summary>
        public const string GenerateSuccess = "qrcode.generate.success";

        /// <summary>Counter metric for failed QR code generations.</summary>
        public const string GenerateFailure = "qrcode.generate.failure";

        /// <summary>Counter metric for cancelled QR code generations.</summary>
        public const string GenerateCancelled = "qrcode.generate.cancelled";
    }
}