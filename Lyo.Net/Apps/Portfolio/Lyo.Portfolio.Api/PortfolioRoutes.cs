namespace Lyo.Portfolio.Api;

/// <summary>Route and DI key constants for the portfolio API host.</summary>
public static class PortfolioRoutes
{
    /// <summary>People HTTP surface.</summary>
    public static class Person
    {
        public const string Route = "Person";
        public const string Address = "PersonAddress";
        public const string PhoneNumber = "PersonPhoneNumber";
        public const string Email = "PersonEmail";
    }

    /// <summary>File storage workbench (Gateway / portfolio demos).</summary>
    public static class FileStorageWorkbench
    {
        public const string Route = "Workbench/FileStorage";
        public const string ServiceKey = "portfolio-filestorage";
        public const string MetadataKey = "portfolio-filestorage-metadata";

        /// <summary>Base route for Lyo.Api Query over Postgres <c>file_metadata</c>.</summary>
        public const string FileMetadata = "Workbench/FileStorage/FileMetadata";
    }

    /// <summary>Root-relative streaming upload (same contract as workbench save-stream).</summary>
    public static class DirectFileUpload
    {
        public const string FilePath = "upload/file";
    }
}
