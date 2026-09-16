using Lyo.Exceptions;

namespace Lyo.HomeInventory.Models;

/// <summary>FileStorage <c>PathPrefix</c> conventions for inventory attachments.</summary>
public static class HomeInventoryFilePaths
{
    /// <summary>Root prefix for all inventory files.</summary>
    public const string Root = "home-inventory";

    /// <summary>Folder for files attached to an item.</summary>
    public static string ForItem(Guid itemId)
    {
        if (itemId == Guid.Empty)
            throw new ArgumentException("Item id is required.", nameof(itemId));

        return $"{Root}/items/{itemId:D}";
    }

    /// <summary>Folder for files attached to a location.</summary>
    public static string ForLocation(Guid locationId)
    {
        if (locationId == Guid.Empty)
            throw new ArgumentException("Location id is required.", nameof(locationId));

        return $"{Root}/locations/{locationId:D}";
    }

    /// <summary>Folder for files attached to a category.</summary>
    public static string ForCategory(Guid categoryId)
    {
        if (categoryId == Guid.Empty)
            throw new ArgumentException("Category id is required.", nameof(categoryId));

        return $"{Root}/categories/{categoryId:D}";
    }

    /// <summary>Resolves the prefix for a Note/Tag subject type and id.</summary>
    public static string ForEntity(string entityType, Guid entityId)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(entityType);
        return entityType switch {
            HomeInventoryEntityTypes.Item => ForItem(entityId),
            HomeInventoryEntityTypes.Location => ForLocation(entityId),
            HomeInventoryEntityTypes.Category => ForCategory(entityId),
            var _ => throw new ArgumentException($"Unsupported inventory entity type '{entityType}'.", nameof(entityType))
        };
    }
}
