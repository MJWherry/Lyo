using Lyo.Common;

namespace Lyo.HomeInventory;

/// <summary>Persists household items (purchases, devices, warranties), categories, storage locations, quantities, and movement history. Listing and search belong in your query layer.</summary>
public interface IHomeInventoryStore
{
#region Movements

    Task<IReadOnlyList<HomeItemMovementRecord>> ListMovementsForItemAsync(Guid itemId, int take = 200, CancellationToken ct = default);

#endregion

#region Items

    Task SaveItemAsync(HomeItemRecord item, CancellationToken ct = default);

    Task<HomeItemRecord?> GetItemByIdAsync(Guid id, CancellationToken ct = default);

    Task<HomeItemRecord?> GetItemBySkuAsync(string sku, CancellationToken ct = default);

    Task DeleteItemAsync(Guid id, CancellationToken ct = default);

#endregion

#region Categories

    Task SaveCategoryAsync(HomeCategoryRecord category, CancellationToken ct = default);

    Task<HomeCategoryRecord?> GetCategoryByIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<HomeCategoryRecord>> ListCategoriesAsync(CancellationToken ct = default);

    Task DeleteCategoryAsync(Guid id, CancellationToken ct = default);

#endregion

#region Locations

    Task SaveLocationAsync(HomeLocationRecord location, CancellationToken ct = default);

    Task<HomeLocationRecord?> GetLocationByIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<HomeLocationRecord>> ListLocationsAsync(bool activeOnly = true, CancellationToken ct = default);

    Task DeleteLocationAsync(Guid id, CancellationToken ct = default);

#endregion

#region Stock

    /// <summary>Creates or replaces quantity rows for an item at a location.</summary>
    Task UpsertStockAsync(Guid itemId, Guid locationId, decimal quantityOnHand, decimal quantityReserved, decimal? reorderPoint, CancellationToken ct = default);

    Task<HomeItemStockRecord?> GetStockAsync(Guid itemId, Guid locationId, CancellationToken ct = default);

    Task<IReadOnlyList<HomeItemStockRecord>> GetStockForItemAsync(Guid itemId, CancellationToken ct = default);

    /// <summary>Applies a delta to on-hand quantity and writes a movement row in one transaction.</summary>
    Task AdjustStockAsync(
        Guid itemId,
        Guid locationId,
        decimal quantityDelta,
        HomeItemMovementType movementType,
        string? referenceNumber,
        string? notes,
        EntityRef? createdBy,
        CancellationToken ct = default);

    /// <summary>Moves quantity between two locations and records a transfer movement.</summary>
    Task TransferStockAsync(
        Guid itemId,
        Guid fromLocationId,
        Guid toLocationId,
        decimal quantity,
        string? referenceNumber,
        string? notes,
        EntityRef? createdBy,
        CancellationToken ct = default);

#endregion
}