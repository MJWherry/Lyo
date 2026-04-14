namespace Lyo.FileStorage;

/// <summary>Consolidated constants for the FileStorage library.</summary>
public static class Constants
{
    /// <summary>Metric names and tags.</summary>
    public static class Metrics
    {
        public const string SaveDuration = "filestorage.save.duration";
        public const string SaveSuccess = "filestorage.save.success";
        public const string SaveCompressed = "filestorage.save.compressed";
        public const string SaveEncrypted = "filestorage.save.encrypted";
        public const string SaveFileSizeBytes = "filestorage.save.file_size_bytes";
        public const string SaveFinalSizeBytes = "filestorage.save.final_size_bytes";
        public const string SaveDurationMs = "filestorage.save.duration_ms";

        public const string GetDuration = "filestorage.get.duration";
        public const string GetSuccess = "filestorage.get.success";
        public const string GetFileSizeBytes = "filestorage.get.file_size_bytes";
        public const string GetDurationMs = "filestorage.get.duration_ms";

        public const string DeleteDuration = "filestorage.delete.duration";
        public const string DeleteSuccess = "filestorage.delete.success";
        public const string DeleteFailure = "filestorage.delete.failure";
        public const string DeleteDurationMs = "filestorage.delete.duration_ms";

        public const string FileSizeBytes = "filestorage.file_size_bytes";

        public const string FileStoragePreSignedUrlGenerated = "filestorage.presigned_url.generated";
        public const string FileStoragePreSignedUrlGenerationFailed = "filestorage.presigned_url.generation_failed";

        public const string AuditAppendFailed = "filestorage.audit.append_failed";
    }
}