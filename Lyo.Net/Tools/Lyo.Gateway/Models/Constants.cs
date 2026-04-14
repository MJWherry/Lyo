namespace Lyo.Gateway.Models;

public static class Constants
{
    public static class Person
    {
        public const string Route = "Person";
        public const string Address = "PersonAddress";
        public const string PhoneNumber = "PersonPhoneNumber";
        public const string Email = "PersonEmail";
    }

    public static class FileStorageWorkbench
    {
        /// <summary>Base route for file metadata Query/QueryProject (same path as Test API <c>Workbench/FileStorage/FileMetadata</c>).</summary>
        public const string FileMetadataRoute = "Workbench/FileStorage/FileMetadata";

        /// <summary>Gateway route for workbench downloads: redirects to a time-limited storage URL from the API when possible (plain files); otherwise streams from Test API.</summary>
        public const string ProxyDownloadRoute = "filestorage-download";
    }
}