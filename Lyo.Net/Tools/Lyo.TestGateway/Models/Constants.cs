namespace Lyo.TestGateway.Models;

public static class Constants
{
    public static class Person
    {
        public const string Route = "Person";
        public const string Address = "PersonAddress";
        public const string PhoneNumber = "PersonPhoneNumber";
        public const string Email = "PersonEmail";
    }

    public static class EndatoPs
    {
        public const string Route = "EndatoPs";
        public const string Person = "EndatoPs/EndatoPsPersonEntity";
        public const string Address = "EndatoPs/EndatoPsAddressEntity";
        public const string PhoneNumber = "EndatoPs/EndatoPsPhoneNumberEntity";
        public const string Email = "EndatoPs/EndatoPsEmailAddressEntity";
    }

    public static class EndatoCe
    {
        public const string Route = "EndatoCe";
        public const string Person = "EndatoCe/EndatoCePersonEntity";
        public const string Address = "EndatoCe/EndatoCeAddressEntity";
        public const string PhoneNumber = "EndatoCe/EndatoCePhoneNumberEntity";
        public const string Email = "EndatoCe/EndatoCeEmailAddressEntity";
    }

    public static class FileStorageWorkbench
    {
        /// <summary>REST prefix for file workbench endpoints on the Test API (save, access links, metadata search, etc.).</summary>
        public const string ApiRoutePrefix = "Workbench/FileStorage";

        /// <summary>Base route for file metadata Query/QueryProject (same path as Test API <c>Workbench/FileStorage/FileMetadata</c>).</summary>
        public const string FileMetadataRoute = "Workbench/FileStorage/FileMetadata";

        /// <summary>Gateway route for workbench downloads: redirects to a time-limited storage URL from the API when possible (plain files); otherwise streams from Test API.</summary>
        public const string ProxyDownloadRoute = "filestorage-download";
    }
}