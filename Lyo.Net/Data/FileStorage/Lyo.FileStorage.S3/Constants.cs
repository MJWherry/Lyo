namespace Lyo.FileStorage.S3;

/// <summary>Consolidated constants for the S3 FileStorage library.</summary>
public static class Constants
{
    /// <summary>Metric names and tags.</summary>
    public static class Metrics
    {
        public const string FileStoragePreSignedUrlGenerated = "filestorage.s3.presigned_url.generated";
        public const string FileStoragePreSignedUrlGenerationFailed = "filestorage.s3.presigned_url.generation_failed";
    }
}