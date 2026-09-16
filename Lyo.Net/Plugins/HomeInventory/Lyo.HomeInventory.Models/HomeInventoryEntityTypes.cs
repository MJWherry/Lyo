namespace Lyo.HomeInventory.Models;

/// <summary>Stable <c>EntityRef.EntityType</c> values for Note/Tag composition. Do not persist CLR type names.</summary>
public static class HomeInventoryEntityTypes
{
    /// <summary>Household item (catalog row).</summary>
    public const string Item = "HomeItem";

    /// <summary>Physical location (room, closet, bin).</summary>
    public const string Location = "HomeLocation";

    /// <summary>Grouping category.</summary>
    public const string Category = "HomeCategory";

    /// <summary>FileStorage attachment link row.</summary>
    public const string Attachment = "HomeAttachment";
}
