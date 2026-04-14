namespace Lyo.FileStorage.Azure;

public static class Constants
{
    public static class Metrics
    {
        public const string FileStoragePreSignedUrlGenerated = "filestorage.azureblob.presigned_url.generated";
        public const string FileStoragePreSignedUrlGenerationFailed = "filestorage.azureblob.presigned_url.generation_failed";
    }
}