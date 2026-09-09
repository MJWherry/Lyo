namespace Lyo.Barcode;

/// <summary>Shared names used across the barcode library.</summary>
public static class Constants
{
    /// <summary>Metric instrument names for barcode generation.</summary>
    public static class Metrics
    {
        public const string GenerateDuration = "barcode.generate.duration";

        public const string BatchGenerateDuration = "barcode.batch.generate.duration";

        public const string GenerateSuccess = "barcode.generate.success";

        public const string GenerateFailure = "barcode.generate.failure";

        public const string GenerateCancelled = "barcode.generate.cancelled";
    }
}