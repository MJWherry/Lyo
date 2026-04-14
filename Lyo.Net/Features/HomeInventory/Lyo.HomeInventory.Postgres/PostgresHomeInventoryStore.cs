using System.Diagnostics;
using Lyo.Common;
using Lyo.Exceptions;
using Lyo.Health;
using Lyo.HomeInventory.Postgres.Database;
using Microsoft.EntityFrameworkCore;

namespace Lyo.HomeInventory.Postgres;

/// <summary>PostgreSQL implementation of <see cref="IHomeInventoryStore" />.</summary>
public sealed class PostgresHomeInventoryStore : IHomeInventoryStore, IHealth
{
    private readonly IDbContextFactory<HomeInventoryDbContext> _contextFactory;

    public PostgresHomeInventoryStore(IDbContextFactory<HomeInventoryDbContext> contextFactory)
    {
        ArgumentHelpers.ThrowIfNull(contextFactory, nameof(contextFactory));
        _contextFactory = contextFactory;
    }

    /// <inheritdoc />
    public string HealthCheckName => "home-inventory-postgres";

    /// <inheritdoc />
    public async Task<HealthResult> CheckHealthAsync(CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        try {
            await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
            var canConnect = await context.Database.CanConnectAsync(ct).ConfigureAwait(false);
            sw.Stop();
            return canConnect
                ? HealthResult.Healthy(sw.Elapsed, null, new Dictionary<string, object?> { ["database"] = "home_inventory" })
                : HealthResult.Unhealthy(sw.Elapsed, "Database connection failed");
        }
        catch (Exception ex) {
            sw.Stop();
            return HealthResult.Unhealthy(sw.Elapsed, ex.Message, null, ex);
        }
    }

    /// <inheritdoc />
    public async Task SaveItemAsync(HomeItemRecord item, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(item, nameof(item));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(item.Name, nameof(item.Name));
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        if (item.Id != default) {
            var existing = await context.Items.FindAsync([item.Id], ct).ConfigureAwait(false);
            if (existing != null) {
                CopyToEntity(item, existing);
                await context.SaveChangesAsync(ct).ConfigureAwait(false);
                return;
            }
        }

        var entity = new HomeItemEntity();
        CopyToEntity(item, entity);
        entity.Id = item.Id == default ? Guid.NewGuid() : item.Id;
        if (entity.CreatedTimestamp == default)
            entity.CreatedTimestamp = DateTime.UtcNow;

        context.Items.Add(entity);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<HomeItemRecord?> GetItemByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await context.Items.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id, ct).ConfigureAwait(false);
        return entity == null ? null : ToItemRecord(entity);
    }

    /// <inheritdoc />
    public async Task<HomeItemRecord?> GetItemBySkuAsync(string sku, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(sku, nameof(sku));
        var trimmed = sku.Trim();
        var lower = trimmed.ToLowerInvariant();
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await context.Items.AsNoTracking().FirstOrDefaultAsync(i => i.Sku != null && i.Sku.ToLower() == lower, ct).ConfigureAwait(false);
        return entity == null ? null : ToItemRecord(entity);
    }

    /// <inheritdoc />
    public async Task DeleteItemAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await context.Items.FindAsync([id], ct).ConfigureAwait(false);
        if (entity != null) {
            context.Items.Remove(entity);
            await context.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task SaveCategoryAsync(HomeCategoryRecord category, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(category, nameof(category));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(category.Name, nameof(category.Name));
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        if (category.Id != default) {
            var existing = await context.Categories.FindAsync([category.Id], ct).ConfigureAwait(false);
            if (existing != null) {
                existing.ParentCategoryId = category.ParentCategoryId;
                existing.Name = category.Name;
                existing.Slug = category.Slug;
                existing.Description = category.Description;
                existing.SortOrder = category.SortOrder;
                await context.SaveChangesAsync(ct).ConfigureAwait(false);
                return;
            }
        }

        var entity = new HomeCategoryEntity {
            Id = category.Id == default ? Guid.NewGuid() : category.Id,
            ParentCategoryId = category.ParentCategoryId,
            Name = category.Name,
            Slug = category.Slug,
            Description = category.Description,
            SortOrder = category.SortOrder,
            CreatedTimestamp = category.CreatedTimestamp == default ? DateTime.UtcNow : category.CreatedTimestamp
        };

        context.Categories.Add(entity);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<HomeCategoryRecord?> GetCategoryByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var e = await context.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct).ConfigureAwait(false);
        return e == null ? null : ToCategoryRecord(e);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<HomeCategoryRecord>> ListCategoriesAsync(CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var list = await context.Categories.AsNoTracking().OrderBy(c => c.SortOrder).ThenBy(c => c.Name).ToListAsync(ct).ConfigureAwait(false);
        return list.Select(ToCategoryRecord).ToList();
    }

    /// <inheritdoc />
    public async Task DeleteCategoryAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        OperationHelpers.ThrowIf(await context.Categories.AnyAsync(c => c.ParentCategoryId == id, ct).ConfigureAwait(false), "Reassign or delete child categories first.");
        OperationHelpers.ThrowIf(await context.Items.AnyAsync(i => i.CategoryId == id, ct).ConfigureAwait(false), "Category is still assigned to items.");
        var entity = await context.Categories.FindAsync([id], ct).ConfigureAwait(false);
        if (entity != null) {
            context.Categories.Remove(entity);
            await context.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task SaveLocationAsync(HomeLocationRecord location, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(location, nameof(location));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(location.Name, nameof(location.Name));
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        if (location.Id != default) {
            var existing = await context.Locations.FindAsync([location.Id], ct).ConfigureAwait(false);
            if (existing != null) {
                existing.ParentLocationId = location.ParentLocationId;
                existing.Name = location.Name;
                existing.Code = location.Code;
                existing.Description = location.Description;
                existing.IsActive = location.IsActive;
                await context.SaveChangesAsync(ct).ConfigureAwait(false);
                return;
            }
        }

        var entity = new HomeLocationEntity {
            Id = location.Id == default ? Guid.NewGuid() : location.Id,
            ParentLocationId = location.ParentLocationId,
            Name = location.Name,
            Code = location.Code,
            Description = location.Description,
            IsActive = location.IsActive,
            CreatedTimestamp = location.CreatedTimestamp == default ? DateTime.UtcNow : location.CreatedTimestamp
        };

        context.Locations.Add(entity);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<HomeLocationRecord?> GetLocationByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var e = await context.Locations.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id, ct).ConfigureAwait(false);
        return e == null ? null : ToLocationRecord(e);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<HomeLocationRecord>> ListLocationsAsync(bool activeOnly = true, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var q = context.Locations.AsNoTracking().AsQueryable();
        if (activeOnly)
            q = q.Where(l => l.IsActive);

        var list = await q.OrderBy(l => l.Name).ToListAsync(ct).ConfigureAwait(false);
        return list.Select(ToLocationRecord).ToList();
    }

    /// <inheritdoc />
    public async Task DeleteLocationAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        OperationHelpers.ThrowIf(await context.Locations.AnyAsync(l => l.ParentLocationId == id, ct).ConfigureAwait(false), "Reassign or delete child locations first.");
        OperationHelpers.ThrowIf(await context.Stocks.AnyAsync(s => s.LocationId == id, ct).ConfigureAwait(false), "Location still has stock rows.");
        OperationHelpers.ThrowIf(
            await context.Movements.AnyAsync(m => m.FromLocationId == id || m.ToLocationId == id, ct).ConfigureAwait(false), "Location is referenced by movement history.");

        var entity = await context.Locations.FindAsync([id], ct).ConfigureAwait(false);
        if (entity != null) {
            context.Locations.Remove(entity);
            await context.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task UpsertStockAsync(Guid itemId, Guid locationId, decimal quantityOnHand, decimal quantityReserved, decimal? reorderPoint, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var row = await context.Stocks.FindAsync([itemId, locationId], ct).ConfigureAwait(false);
        var now = DateTime.UtcNow;
        if (row == null) {
            context.Stocks.Add(
                new() {
                    ItemId = itemId,
                    LocationId = locationId,
                    QuantityOnHand = quantityOnHand,
                    QuantityReserved = quantityReserved,
                    ReorderPoint = reorderPoint,
                    UpdatedTimestamp = now
                });
        }
        else {
            row.QuantityOnHand = quantityOnHand;
            row.QuantityReserved = quantityReserved;
            row.ReorderPoint = reorderPoint;
            row.UpdatedTimestamp = now;
        }

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<HomeItemStockRecord?> GetStockAsync(Guid itemId, Guid locationId, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var row = await context.Stocks.AsNoTracking().FirstOrDefaultAsync(s => s.ItemId == itemId && s.LocationId == locationId, ct).ConfigureAwait(false);
        return row == null ? null : ToStockRecord(row);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<HomeItemStockRecord>> GetStockForItemAsync(Guid itemId, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var list = await context.Stocks.AsNoTracking().Where(s => s.ItemId == itemId).ToListAsync(ct).ConfigureAwait(false);
        return list.Select(ToStockRecord).ToList();
    }

    /// <inheritdoc />
    public async Task AdjustStockAsync(
        Guid itemId,
        Guid locationId,
        decimal quantityDelta,
        HomeItemMovementType movementType,
        string? referenceNumber,
        string? notes,
        EntityRef? createdBy,
        CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        await using var tx = await context.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
        var row = await context.Stocks.FindAsync([itemId, locationId], ct).ConfigureAwait(false);
        if (row == null) {
            if (quantityDelta <= 0)
                OperationHelpers.ThrowIf(true, "Cannot adjust stock at a location that has no balance when the delta is not positive.");

            row = new() {
                ItemId = itemId,
                LocationId = locationId,
                QuantityOnHand = quantityDelta,
                QuantityReserved = 0,
                ReorderPoint = null,
                UpdatedTimestamp = DateTime.UtcNow
            };

            context.Stocks.Add(row);
        }
        else {
            var next = row.QuantityOnHand + quantityDelta;
            OperationHelpers.ThrowIf(next < 0, "Adjustment would make quantity on hand negative.");
            row.QuantityOnHand = next;
            row.UpdatedTimestamp = DateTime.UtcNow;
        }

        var magnitude = Math.Abs(quantityDelta);
        if (quantityDelta >= 0)
            context.Movements.Add(CreateMovementEntity(itemId, movementType, magnitude, null, locationId, referenceNumber, notes, createdBy));
        else
            context.Movements.Add(CreateMovementEntity(itemId, movementType, magnitude, locationId, null, referenceNumber, notes, createdBy));

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        await tx.CommitAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task TransferStockAsync(
        Guid itemId,
        Guid fromLocationId,
        Guid toLocationId,
        decimal quantity,
        string? referenceNumber,
        string? notes,
        EntityRef? createdBy,
        CancellationToken ct = default)
    {
        OperationHelpers.ThrowIf(quantity <= 0, "Transfer quantity must be positive.");
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        await using var tx = await context.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
        var fromRow = await context.Stocks.FindAsync([itemId, fromLocationId], ct).ConfigureAwait(false);
        var sourceRow = fromRow ?? throw new InvalidOperationException("No stock row at the source location.");
        OperationHelpers.ThrowIf(sourceRow.QuantityOnHand < quantity, "Insufficient quantity at the source location.");
        sourceRow.QuantityOnHand -= quantity;
        sourceRow.UpdatedTimestamp = DateTime.UtcNow;
        var toRow = await context.Stocks.FindAsync([itemId, toLocationId], ct).ConfigureAwait(false);
        if (toRow == null) {
            toRow = new() {
                ItemId = itemId,
                LocationId = toLocationId,
                QuantityOnHand = quantity,
                QuantityReserved = 0,
                ReorderPoint = null,
                UpdatedTimestamp = DateTime.UtcNow
            };

            context.Stocks.Add(toRow);
        }
        else {
            toRow.QuantityOnHand += quantity;
            toRow.UpdatedTimestamp = DateTime.UtcNow;
        }

        context.Movements.Add(CreateMovementEntity(itemId, HomeItemMovementType.StockTransfer, quantity, fromLocationId, toLocationId, referenceNumber, notes, createdBy));
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        await tx.CommitAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<HomeItemMovementRecord>> ListMovementsForItemAsync(Guid itemId, int take = 200, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var q = context.Movements.AsNoTracking().Where(m => m.ItemId == itemId).OrderByDescending(m => m.CreatedTimestamp);
        var list = take > 0 ? await q.Take(take).ToListAsync(ct).ConfigureAwait(false) : await q.ToListAsync(ct).ConfigureAwait(false);
        return list.Select(ToMovementRecord).ToList();
    }

    private static HomeItemMovementEntity CreateMovementEntity(
        Guid itemId,
        HomeItemMovementType type,
        decimal quantity,
        Guid? fromLocationId,
        Guid? toLocationId,
        string? referenceNumber,
        string? notes,
        EntityRef? createdBy)
        => new() {
            Id = Guid.NewGuid(),
            ItemId = itemId,
            MovementType = (int)type,
            Quantity = quantity,
            FromLocationId = fromLocationId,
            ToLocationId = toLocationId,
            ReferenceNumber = referenceNumber,
            Notes = notes,
            CreatedByEntityType = createdBy?.EntityType,
            CreatedByEntityId = createdBy?.EntityId,
            CreatedTimestamp = DateTime.UtcNow
        };

    private static void CopyToEntity(HomeItemRecord item, HomeItemEntity e)
    {
        e.OwnerEntityType = item.OwnerEntityType;
        e.OwnerEntityId = item.OwnerEntityId;
        e.CategoryId = item.CategoryId;
        e.ParentItemId = item.ParentItemId;
        e.Name = item.Name;
        e.Description = item.Description;
        e.Notes = item.Notes;
        e.Status = (int)item.Status;
        e.Condition = (int)item.Condition;
        e.Sku = item.Sku;
        e.PurchaseOrderNumber = item.PurchaseOrderNumber;
        e.SalesOrderNumber = item.SalesOrderNumber;
        e.Manufacturer = item.Manufacturer;
        e.ManufacturerPartNumber = item.ManufacturerPartNumber;
        e.Seller = item.Seller;
        e.VendorSku = item.VendorSku;
        e.Upc = item.Upc;
        e.Ean = item.Ean;
        e.Isbn = item.Isbn;
        e.ModelNumber = item.ModelNumber;
        e.Color = item.Color;
        e.SerialNumber = item.SerialNumber;
        e.Imei = item.Imei;
        e.EthernetMacAddress = item.EthernetMacAddress;
        e.WifiMacAddress = item.WifiMacAddress;
        e.BluetoothMacAddress = item.BluetoothMacAddress;
        e.Msrp = item.Msrp;
        e.Cost = item.Cost;
        e.Currency = item.Currency;
        e.WeightGrams = item.WeightGrams;
        e.LengthMm = item.LengthMm;
        e.WidthMm = item.WidthMm;
        e.HeightMm = item.HeightMm;
        e.AcquiredDate = item.AcquiredDate;
        e.WarrantyExpires = item.WarrantyExpires;
        e.CountryOfOrigin = item.CountryOfOrigin;
        e.LotNumber = item.LotNumber;
        e.BatchNumber = item.BatchNumber;
        e.CustomAttributesJson = item.CustomAttributesJson;
    }

    private static HomeItemRecord ToItemRecord(HomeItemEntity e)
        => new() {
            Id = e.Id,
            OwnerEntityType = e.OwnerEntityType,
            OwnerEntityId = e.OwnerEntityId,
            CategoryId = e.CategoryId,
            ParentItemId = e.ParentItemId,
            Name = e.Name,
            Description = e.Description,
            Notes = e.Notes,
            Status = (HomeItemStatus)e.Status,
            Condition = (HomeItemCondition)e.Condition,
            Sku = e.Sku,
            PurchaseOrderNumber = e.PurchaseOrderNumber,
            SalesOrderNumber = e.SalesOrderNumber,
            Manufacturer = e.Manufacturer,
            ManufacturerPartNumber = e.ManufacturerPartNumber,
            Seller = e.Seller,
            VendorSku = e.VendorSku,
            Upc = e.Upc,
            Ean = e.Ean,
            Isbn = e.Isbn,
            ModelNumber = e.ModelNumber,
            Color = e.Color,
            SerialNumber = e.SerialNumber,
            Imei = e.Imei,
            EthernetMacAddress = e.EthernetMacAddress,
            WifiMacAddress = e.WifiMacAddress,
            BluetoothMacAddress = e.BluetoothMacAddress,
            Msrp = e.Msrp,
            Cost = e.Cost,
            Currency = e.Currency,
            WeightGrams = e.WeightGrams,
            LengthMm = e.LengthMm,
            WidthMm = e.WidthMm,
            HeightMm = e.HeightMm,
            AcquiredDate = e.AcquiredDate,
            WarrantyExpires = e.WarrantyExpires,
            CountryOfOrigin = e.CountryOfOrigin,
            LotNumber = e.LotNumber,
            BatchNumber = e.BatchNumber,
            CustomAttributesJson = e.CustomAttributesJson,
            CreatedTimestamp = e.CreatedTimestamp,
            UpdatedTimestamp = e.UpdatedTimestamp
        };

    private static HomeCategoryRecord ToCategoryRecord(HomeCategoryEntity e)
        => new() {
            Id = e.Id,
            ParentCategoryId = e.ParentCategoryId,
            Name = e.Name,
            Slug = e.Slug,
            Description = e.Description,
            SortOrder = e.SortOrder,
            CreatedTimestamp = e.CreatedTimestamp,
            UpdatedTimestamp = e.UpdatedTimestamp
        };

    private static HomeLocationRecord ToLocationRecord(HomeLocationEntity e)
        => new() {
            Id = e.Id,
            ParentLocationId = e.ParentLocationId,
            Name = e.Name,
            Code = e.Code,
            Description = e.Description,
            IsActive = e.IsActive,
            CreatedTimestamp = e.CreatedTimestamp,
            UpdatedTimestamp = e.UpdatedTimestamp
        };

    private static HomeItemStockRecord ToStockRecord(HomeItemStockEntity e)
        => new() {
            ItemId = e.ItemId,
            LocationId = e.LocationId,
            QuantityOnHand = e.QuantityOnHand,
            QuantityReserved = e.QuantityReserved,
            ReorderPoint = e.ReorderPoint,
            UpdatedTimestamp = e.UpdatedTimestamp
        };

    private static HomeItemMovementRecord ToMovementRecord(HomeItemMovementEntity e)
        => new() {
            Id = e.Id,
            ItemId = e.ItemId,
            MovementType = (HomeItemMovementType)e.MovementType,
            Quantity = e.Quantity,
            FromLocationId = e.FromLocationId,
            ToLocationId = e.ToLocationId,
            ReferenceNumber = e.ReferenceNumber,
            Notes = e.Notes,
            CreatedByEntityType = e.CreatedByEntityType,
            CreatedByEntityId = e.CreatedByEntityId,
            CreatedTimestamp = e.CreatedTimestamp
        };
}