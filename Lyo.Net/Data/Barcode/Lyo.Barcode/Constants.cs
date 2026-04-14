namespace Lyo.Barcode;

/// <summary>Consolidated constants for the Barcode library.</summary>
public static class Constants
{
    /// <summary>Constants for barcode service metrics.</summary>
    public static class Metrics
    {
        public const string GenerateDuration = "barcode.generate.duration";

        public const string BatchGenerateDuration = "barcode.batch.generate.duration";

        public const string GenerateSuccess = "barcode.generate.success";

        public const string GenerateFailure = "barcode.generate.failure";

        public const string GenerateCancelled = "barcode.generate.cancelled";
    }
}