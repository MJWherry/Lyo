namespace Lyo.QRCode;

/// <summary>Shared constants for the QRCode library.</summary>
public static class Constants
{
    /// <summary>Metric names for QR service operations.</summary>
    public static class Metrics
    {
        /// <summary>Duration metric for a single QR generation.</summary>
        public const string GenerateDuration = "qrcode.generate.duration";

        /// <summary>Duration metric for a batch QR generation.</summary>
        public const string BatchGenerateDuration = "qrcode.batch.generate.duration";

        /// <summary>Counter for successful QR generations.</summary>
        public const string GenerateSuccess = "qrcode.generate.success";

        /// <summary>Counter for failed QR generations.</summary>
        public const string GenerateFailure = "qrcode.generate.failure";

        /// <summary>Counter for cancelled QR generations.</summary>
        public const string GenerateCancelled = "qrcode.generate.cancelled";
    }
}