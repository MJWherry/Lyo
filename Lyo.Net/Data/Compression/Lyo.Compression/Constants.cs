namespace Lyo.Compression;

/// <summary>Consolidated constants for the Compression library.</summary>
public static class Constants
{
    /// <summary>Metric names and tags.</summary>
    public static class Metrics
    {
        public const string CompressDuration = "compression.compress.duration";
        public const string CompressSuccess = "compression.compress.success";
        public const string CompressFailure = "compression.compress.failure";
        public const string CompressRatio = "compression.compress.ratio";
        public const string CompressInputSizeBytes = "compression.compress.input_size_bytes";
        public const string CompressOutputSizeBytes = "compression.compress.output_size_bytes";
        public const string CompressDurationMs = "compression.compress.duration_ms";

        public const string DecompressDuration = "compression.decompress.duration";
        public const string DecompressSuccess = "compression.decompress.success";
        public const string DecompressFailure = "compression.decompress.failure";
        public const string DecompressInputSizeBytes = "compression.decompress.input_size_bytes";
        public const string DecompressOutputSizeBytes = "compression.decompress.output_size_bytes";
        public const string DecompressDurationMs = "compression.decompress.duration_ms";

        public const string CompressFileDuration = "compression.compress_file.duration";
        public const string CompressFileSuccess = "compression.compress_file.success";
        public const string CompressFileFailure = "compression.compress_file.failure";
        public const string CompressFileRatio = "compression.compress_file.ratio";
        public const string CompressFileInputSizeBytes = "compression.compress_file.input_size_bytes";
        public const string CompressFileOutputSizeBytes = "compression.compress_file.output_size_bytes";
        public const string CompressFileDurationMs = "compression.compress_file.duration_ms";

        public const string DecompressFileDuration = "compression.decompress_file.duration";
        public const string DecompressFileSuccess = "compression.decompress_file.success";
        public const string DecompressFileFailure = "compression.decompress_file.failure";
        public const string DecompressFileInputSizeBytes = "compression.decompress_file.input_size_bytes";
        public const string DecompressFileOutputSizeBytes = "compression.decompress_file.output_size_bytes";
        public const string DecompressFileDurationMs = "compression.decompress_file.duration_ms";
    }
}