using Lyo.EntityReference.Models;
using Lyo.HomeInventory.Models.Enums;

namespace Lyo.HomeInventory;

/// <summary>
/// Stores household items (purchases, devices, warranties), categories, storage locations, quantities, attachments, and movement history. Listing and search belong in your
/// query layer. Free-text comments and labels go through <c>INoteStore</c> / <c>ITagStore</c>.
/// </summary>
public interface IHomeInventoryStore
{
#region Movements

    /// <summary>Recent movements for an item, newest first.</summary>
    Task<IReadOnlyList<HomeItemMovementRecord>> ListMovementsForItemAsync(Guid itemId, int take, Guid? tenantId, CancellationToken ct = default);

#endregion

#region Items

    /// <summary>Upserts an item. Generates <see cref="HomeItemRecord.Id" /> when empty.</summary>
    Task SaveItemAsync(HomeItemRecord item, Guid? tenantId, CancellationToken ct = default);

    /// <summary>One item by id.</summary>
    Task<HomeItemRecord?> GetItemByIdAsync(Guid id, Guid? tenantId, CancellationToken ct = default);

    /// <summary>Hard-deletes an item (stock, movements, and attachment rows cascade).</summary>
    Task DeleteItemAsync(Guid id, Guid? tenantId, CancellationToken ct = default);

#endregion

#region Categories

    /// <summary>Upserts a category.</summary>
    Task SaveCategoryAsync(HomeCategoryRecord category, Guid? tenantId, CancellationToken ct = default);

    /// <summary>One category by id.</summary>
    Task<HomeCategoryRecord?> GetCategoryByIdAsync(Guid id, Guid? tenantId, CancellationToken ct = default);

    /// <summary>All categories for the tenant.</summary>
    Task<IReadOnlyList<HomeCategoryRecord>> ListCategoriesAsync(Guid? tenantId, CancellationToken ct = default);

    /// <summary>Deletes a category when it has no children or items.</summary>
    Task DeleteCategoryAsync(Guid id, Guid? tenantId, CancellationToken ct = default);

#endregion

#region Locations

    /// <summary>Upserts a location.</summary>
    Task SaveLocationAsync(HomeLocationRecord location, Guid? tenantId, CancellationToken ct = default);

    /// <summary>One location by id.</summary>
    Task<HomeLocationRecord?> GetLocationByIdAsync(Guid id, Guid? tenantId, CancellationToken ct = default);

    /// <summary>Locations for the tenant. When <paramref name="activeOnly" /> is true, inactive rows are omitted.</summary>
    Task<IReadOnlyList<HomeLocationRecord>> ListLocationsAsync(bool activeOnly, Guid? tenantId, CancellationToken ct = default);

    /// <summary>Deletes a location when it has no children, stock, or movements. Assigned items have <see cref="HomeItemRecord.LocationId" /> set to null.</summary>
    Task DeleteLocationAsync(Guid id, Guid? tenantId, CancellationToken ct = default);

#endregion

#region Stock

    /// <summary>Writes or replaces quantity rows for an item at a location.</summary>
    Task UpsertStockAsync(Guid itemId, Guid locationId, decimal quantityOnHand, decimal quantityReserved, decimal? reorderPoint, Guid? tenantId, CancellationToken ct = default);

    /// <summary>Stock for one item at one location.</summary>
    Task<HomeItemStockRecord?> GetStockAsync(Guid itemId, Guid locationId, Guid? tenantId, CancellationToken ct = default);

    /// <summary>All stock rows for an item.</summary>
    Task<IReadOnlyList<HomeItemStockRecord>> GetStockForItemAsync(Guid itemId, Guid? tenantId, CancellationToken ct = default);

    /// <summary>Applies an on-hand quantity delta and writes a movement row in one transaction.</summary>
    Task AdjustStockAsync(
        Guid itemId,
        Guid locationId,
        decimal quantityDelta,
        HomeItemMovementType movementType,
        string? referenceNumber,
        EntityRef? createdBy,
        Guid? tenantId,
        CancellationToken ct = default);

    /// <summary>Transfers quantity between two locations and records a transfer movement.</summary>
    Task TransferStockAsync(
        Guid itemId,
        Guid fromLocationId,
        Guid toLocationId,
        decimal quantity,
        string? referenceNumber,
        EntityRef? createdBy,
        Guid? tenantId,
        CancellationToken ct = default);

#endregion

#region Attachments

    /// <summary>Upserts an attachment link. Generates <see cref="HomeAttachmentRecord.Id" /> when empty.</summary>
    Task SaveAttachmentAsync(HomeAttachmentRecord attachment, Guid? tenantId, CancellationToken ct = default);

    /// <summary>One attachment by id.</summary>
    Task<HomeAttachmentRecord?> GetAttachmentByIdAsync(Guid id, Guid? tenantId, CancellationToken ct = default);

    /// <summary>Attachments for a subject, ordered by <see cref="HomeAttachmentRecord.SortOrder" /> then created.</summary>
    Task<IReadOnlyList<HomeAttachmentRecord>> ListAttachmentsAsync(string entityType, Guid entityId, Guid? tenantId, CancellationToken ct = default);

    /// <summary>Removes an attachment link. FileStorage bytes are left in place unless a FileStorage helper deletes them.</summary>
    Task DeleteAttachmentAsync(Guid id, Guid? tenantId, CancellationToken ct = default);

#endregion
}