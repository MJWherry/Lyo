namespace Lyo.TestApi;

public static class Constants
{
    public static class Person
    {
        public const string Route = "Person";
        public const string Address = "PersonAddress";
        public const string PhoneNumber = "PersonPhoneNumber";
        public const string Email = "PersonEmail";
    }

    public static class Twilio
    {
        public const string Route = "Twilio";
        public const string SmsLog = "TwilioSmsLog";
    }

    public static class FileStorageWorkbench
    {
        public const string Route = "Workbench/FileStorage";
        public const string ServiceKey = "gateway-filestorage";
        public const string MetadataKey = "gateway-filestorage-metadata";

        /// <summary>Base route for Lyo.Api Query/QueryProject over Postgres <c>file_metadata</c> (append <c>/Query</c>, <c>/QueryProject</c>).</summary>
        public const string FileMetadata = "Workbench/FileStorage/FileMetadata";
    }

    /// <summary>Root-relative streaming upload (same query + multipart contract as <see cref="FileStorageWorkbench.Route" />/files/save-stream).</summary>
    public static class DirectFileUpload
    {
        public const string FilePath = "upload/file";
    }
}