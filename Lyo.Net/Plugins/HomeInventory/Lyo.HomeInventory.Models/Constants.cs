namespace Lyo.HomeInventory.Models;

/// <summary>Shared constants for the HomeInventory HTTP surface.</summary>
public static class Constants
{
    /// <summary>REST API route names.</summary>
    public static class Rest
    {
        /// <summary>HomeInventory route prefixes used by <c>BuildHomeInventoryGroup</c> and Blazor clients.</summary>
        public static class HomeInventory
        {
            /// <summary>OpenAPI tag and route root.</summary>
            public const string Route = "HomeInventory";

            /// <summary>Category CRUD.</summary>
            public const string Categories = $"{Route}/Categories";

            /// <summary>Location (room / bin) CRUD.</summary>
            public const string Locations = $"{Route}/Locations";

            /// <summary>Item CRUD.</summary>
            public const string Items = $"{Route}/Items";

            /// <summary>Read-only movement Query/Get.</summary>
            public const string Movements = $"{Route}/Movements";

            /// <summary>Attachment link CRUD.</summary>
            public const string Attachments = $"{Route}/Attachments";
        }
    }
}
