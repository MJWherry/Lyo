namespace Lyo.FileStorage.S3;

/// <summary>Shared constants for the S3 FileStorage library.</summary>
public static class Constants
{
    /// <summary>Metric names used by S3 operations.</summary>
    public static class Metrics
    {
        public const string FileStoragePreSignedUrlGenerated = "filestorage.s3.presigned_url.generated";
        public const string FileStoragePreSignedUrlGenerationFailed = "filestorage.s3.presigned_url.generation_failed";
    }
}