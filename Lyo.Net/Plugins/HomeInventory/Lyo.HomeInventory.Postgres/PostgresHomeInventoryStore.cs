using Lyo.Common.Core.Identifiers;
using Lyo.EntityReference.Models;
using Lyo.EntityReference.Postgres;
using Lyo.Exceptions;
using Lyo.Health;
using Lyo.HomeInventory.Models;
using Lyo.HomeInventory.Models.Enums;
using Lyo.HomeInventory.Postgres.Database;
using Lyo.Postgres;
using Microsoft.EntityFrameworkCore;

namespace Lyo.HomeInventory.Postgres;

/// <summary>Postgres-backed <see cref="IHomeInventoryStore" />.</summary>
public sealed class PostgresHomeInventoryStore : IHomeInventoryStore, IHealth
{
    private readonly IDbContextFactory<HomeInventoryDbContext> _contextFactory;
    private readonly EntityRefOptions _entityRefOptions;
    private readonly TenancyOptions _featureTenancy;

    public PostgresHomeInventoryStore(
        IDbContextFactory<HomeInventoryDbContext> contextFactory,
        EntityRefOptions entityRefOptions,
        PostgresHomeInventoryOptions homeInventoryOptions)
    {
        ArgumentHelpers.ThrowIfNull(contextFactory);
        ArgumentHelpers.ThrowIfNull(entityRefOptions);
        ArgumentHelpers.ThrowIfNull(homeInventoryOptions);
        _contextFactory = contextFactory;
        _entityRefOptions = entityRefOptions;
        _featureTenancy = homeInventoryOptions.Tenancy;
    }

    /// <inheritdoc />
    public string HealthCheckName => "home-inventory-postgres";

    /// <inheritdoc />
    public Task<HealthResult> CheckHealthAsync(CancellationToken ct = default) => PostgresHealth.CheckAsync(_contextFactory, PostgresHomeInventoryOptions.Schema, ct);

    /// <inheritdoc />
    public async Task SaveItemAsync(HomeItemRecord item, Guid? tenantId, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(item);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(item.Name, nameof(item.Name));
        var resolvedTenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        if (item.Id != Guid.Empty) {
            var existing = await context.Items.FirstOrDefaultAsync(i => i.Id == item.Id && i.TenantId == resolvedTenant, ct).ConfigureAwait(false);
            if (existing != null) {
                CopyToEntity(item, existing);
                existing.TenantId = resolvedTenant;
                await context.SaveChangesAsync(ct).ConfigureAwait(false);
                return;
            }
        }

        var entity = new HomeItemEntity();
        CopyToEntity(item, entity);
        entity.Id = item.Id == Guid.Empty ? LyoGuid.CreateV7() : item.Id;
        entity.TenantId = resolvedTenant;
        if (entity.CreatedTimestamp == default)
            entity.CreatedTimestamp = DateTime.UtcNow;

        context.Items.Add(entity);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<HomeItemRecord?> GetItemByIdAsync(Guid id, Guid? tenantId, CancellationToken ct = default)
    {
        var resolvedTenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await context.Items.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id && i.TenantId == resolvedTenant, ct).ConfigureAwait(false);
        return entity == null ? null : ToItemRecord(entity);
    }

    /// <inheritdoc />
    public async Task DeleteItemAsync(Guid id, Guid? tenantId, CancellationToken ct = default)
    {
        var resolvedTenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await context.Items.FirstOrDefaultAsync(i => i.Id == id && i.TenantId == resolvedTenant, ct).ConfigureAwait(false);
        if (entity != null) {
            await RemoveAttachmentsAsync(context, HomeInventoryEntityTypes.Item, id, resolvedTenant, ct).ConfigureAwait(false);
            context.Items.Remove(entity);
            await context.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task SaveCategoryAsync(HomeCategoryRecord category, Guid? tenantId, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(category);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(category.Name, nameof(category.Name));
        var resolvedTenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        if (category.Id != Guid.Empty) {
            var existing = await context.Categories.FirstOrDefaultAsync(c => c.Id == category.Id && c.TenantId == resolvedTenant, ct).ConfigureAwait(false);
            if (existing != null) {
                existing.ParentCategoryId = category.ParentCategoryId;
                existing.Name = category.Name;
                existing.Slug = category.Slug;
                existing.Description = category.Description;
                existing.SortOrder = category.SortOrder;
                existing.TenantId = resolvedTenant;
                await context.SaveChangesAsync(ct).ConfigureAwait(false);
                return;
            }
        }

        var entity = new HomeCategoryEntity {
            Id = category.Id == Guid.Empty ? LyoGuid.CreateV7() : category.Id,
            ParentCategoryId = category.ParentCategoryId,
            Name = category.Name,
            Slug = category.Slug,
            Description = category.Description,
            SortOrder = category.SortOrder,
            TenantId = resolvedTenant,
            CreatedTimestamp = category.CreatedTimestamp == default ? DateTime.UtcNow : category.CreatedTimestamp
        };

        context.Categories.Add(entity);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<HomeCategoryRecord?> GetCategoryByIdAsync(Guid id, Guid? tenantId, CancellationToken ct = default)
    {
        var resolvedTenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var e = await context.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id && c.TenantId == resolvedTenant, ct).ConfigureAwait(false);
        return e == null ? null : ToCategoryRecord(e);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<HomeCategoryRecord>> ListCategoriesAsync(Guid? tenantId, CancellationToken ct = default)
    {
        var resolvedTenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var list = await context.Categories.AsNoTracking()
            .Where(c => c.TenantId == resolvedTenant)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return list.Select(ToCategoryRecord).ToList();
    }

    /// <inheritdoc />
    public async Task DeleteCategoryAsync(Guid id, Guid? tenantId, CancellationToken ct = default)
    {
        var resolvedTenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        OperationHelpers.ThrowIf(
            await context.Categories.AnyAsync(c => c.ParentCategoryId == id && c.TenantId == resolvedTenant, ct).ConfigureAwait(false),
            "Reassign or delete child categories first.");

        OperationHelpers.ThrowIf(
            await context.Items.AnyAsync(i => i.CategoryId == id && i.TenantId == resolvedTenant, ct).ConfigureAwait(false), "Category is still assigned to items.");

        var entity = await context.Categories.FirstOrDefaultAsync(c => c.Id == id && c.TenantId == resolvedTenant, ct).ConfigureAwait(false);
        if (entity != null) {
            await RemoveAttachmentsAsync(context, HomeInventoryEntityTypes.Category, id, resolvedTenant, ct).ConfigureAwait(false);
            context.Categories.Remove(entity);
            await context.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task SaveLocationAsync(HomeLocationRecord location, Guid? tenantId, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(location);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(location.Name, nameof(location.Name));
        var resolvedTenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        if (location.Id != Guid.Empty) {
            var existing = await context.Locations.FirstOrDefaultAsync(l => l.Id == location.Id && l.TenantId == resolvedTenant, ct).ConfigureAwait(false);
            if (existing != null) {
                existing.ParentLocationId = location.ParentLocationId;
                existing.Name = location.Name;
                existing.Code = location.Code;
                existing.Description = location.Description;
                existing.IsActive = location.IsActive;
                existing.TenantId = resolvedTenant;
                await context.SaveChangesAsync(ct).ConfigureAwait(false);
                return;
            }
        }

        var entity = new HomeLocationEntity {
            Id = location.Id == Guid.Empty ? LyoGuid.CreateV7() : location.Id,
            ParentLocationId = location.ParentLocationId,
            Name = location.Name,
            Code = location.Code,
            Description = location.Description,
            IsActive = location.IsActive,
            TenantId = resolvedTenant,
            CreatedTimestamp = location.CreatedTimestamp == default ? DateTime.UtcNow : location.CreatedTimestamp
        };

        context.Locations.Add(entity);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<HomeLocationRecord?> GetLocationByIdAsync(Guid id, Guid? tenantId, CancellationToken ct = default)
    {
        var resolvedTenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var e = await context.Locations.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id && l.TenantId == resolvedTenant, ct).ConfigureAwait(false);
        return e == null ? null : ToLocationRecord(e);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<HomeLocationRecord>> ListLocationsAsync(bool activeOnly, Guid? tenantId, CancellationToken ct = default)
    {
        var resolvedTenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var q = context.Locations.AsNoTracking().Where(l => l.TenantId == resolvedTenant);
        if (activeOnly)
            q = q.Where(l => l.IsActive);

        var list = await q.OrderBy(l => l.Name).ToListAsync(ct).ConfigureAwait(false);
        return list.Select(ToLocationRecord).ToList();
    }

    /// <inheritdoc />
    public async Task DeleteLocationAsync(Guid id, Guid? tenantId, CancellationToken ct = default)
    {
        var resolvedTenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        OperationHelpers.ThrowIf(
            await context.Locations.AnyAsync(l => l.ParentLocationId == id && l.TenantId == resolvedTenant, ct).ConfigureAwait(false), "Reassign or delete child locations first.");

        OperationHelpers.ThrowIf(
            await context.Stocks.AnyAsync(s => s.LocationId == id && s.TenantId == resolvedTenant, ct).ConfigureAwait(false), "Location still has stock rows.");

        OperationHelpers.ThrowIf(
            await context.Movements.AnyAsync(m => (m.FromLocationId == id || m.ToLocationId == id) && m.TenantId == resolvedTenant, ct).ConfigureAwait(false),
            "Location is referenced by movement history.");

        var entity = await context.Locations.FirstOrDefaultAsync(l => l.Id == id && l.TenantId == resolvedTenant, ct).ConfigureAwait(false);
        if (entity != null) {
            await RemoveAttachmentsAsync(context, HomeInventoryEntityTypes.Location, id, resolvedTenant, ct).ConfigureAwait(false);
            context.Locations.Remove(entity);
            await context.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task UpsertStockAsync(
        Guid itemId,
        Guid locationId,
        decimal quantityOnHand,
        decimal quantityReserved,
        decimal? reorderPoint,
        Guid? tenantId,
        CancellationToken ct = default)
    {
        var resolvedTenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var row = await context.Stocks.FirstOrDefaultAsync(s => s.ItemId == itemId && s.LocationId == locationId && s.TenantId == resolvedTenant, ct).ConfigureAwait(false);
        var now = DateTime.UtcNow;
        if (row == null) {
            context.Stocks.Add(
                new() {
                    ItemId = itemId,
                    LocationId = locationId,
                    QuantityOnHand = quantityOnHand,
                    QuantityReserved = quantityReserved,
                    ReorderPoint = reorderPoint,
                    TenantId = resolvedTenant,
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
    public async Task<HomeItemStockRecord?> GetStockAsync(Guid itemId, Guid locationId, Guid? tenantId, CancellationToken ct = default)
    {
        var resolvedTenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var row = await context.Stocks.AsNoTracking()
            .FirstOrDefaultAsync(s => s.ItemId == itemId && s.LocationId == locationId && s.TenantId == resolvedTenant, ct)
            .ConfigureAwait(false);

        return row == null ? null : ToStockRecord(row);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<HomeItemStockRecord>> GetStockForItemAsync(Guid itemId, Guid? tenantId, CancellationToken ct = default)
    {
        var resolvedTenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var list = await context.Stocks.AsNoTracking().Where(s => s.ItemId == itemId && s.TenantId == resolvedTenant).ToListAsync(ct).ConfigureAwait(false);
        return list.Select(ToStockRecord).ToList();
    }

    /// <inheritdoc />
    public async Task AdjustStockAsync(
        Guid itemId,
        Guid locationId,
        decimal quantityDelta,
        HomeItemMovementType movementType,
        string? referenceNumber,
        EntityRef? createdBy,
        Guid? tenantId,
        CancellationToken ct = default)
    {
        var resolvedTenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        await using var tx = await context.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
        var row = await context.Stocks.FirstOrDefaultAsync(s => s.ItemId == itemId && s.LocationId == locationId && s.TenantId == resolvedTenant, ct).ConfigureAwait(false);
        if (row == null) {
            if (quantityDelta <= 0)
                OperationHelpers.ThrowIf(true, "Cannot adjust stock at a location that has no balance when the delta is not positive.");

            row = new() {
                ItemId = itemId,
                LocationId = locationId,
                QuantityOnHand = quantityDelta,
                QuantityReserved = 0,
                ReorderPoint = null,
                TenantId = resolvedTenant,
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
        context.Movements.Add(
            quantityDelta >= 0
                ? CreateMovementEntity(itemId, movementType, magnitude, null, locationId, referenceNumber, createdBy, resolvedTenant)
                : CreateMovementEntity(itemId, movementType, magnitude, locationId, null, referenceNumber, createdBy, resolvedTenant));

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
        EntityRef? createdBy,
        Guid? tenantId,
        CancellationToken ct = default)
    {
        OperationHelpers.ThrowIf(quantity <= 0, "Transfer quantity must be positive.");
        var resolvedTenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        await using var tx = await context.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
        var fromRow = await context.Stocks.FirstOrDefaultAsync(s => s.ItemId == itemId && s.LocationId == fromLocationId && s.TenantId == resolvedTenant, ct).ConfigureAwait(false);
        OperationHelpers.ThrowIfNull(fromRow, "No stock row at the source location.");
        var sourceRow = fromRow;
        OperationHelpers.ThrowIf(sourceRow.QuantityOnHand < quantity, "Insufficient quantity at the source location.");
        sourceRow.QuantityOnHand -= quantity;
        sourceRow.UpdatedTimestamp = DateTime.UtcNow;
        var toRow = await context.Stocks.FirstOrDefaultAsync(s => s.ItemId == itemId && s.LocationId == toLocationId && s.TenantId == resolvedTenant, ct).ConfigureAwait(false);
        if (toRow == null) {
            toRow = new() {
                ItemId = itemId,
                LocationId = toLocationId,
                QuantityOnHand = quantity,
                QuantityReserved = 0,
                ReorderPoint = null,
                TenantId = resolvedTenant,
                UpdatedTimestamp = DateTime.UtcNow
            };

            context.Stocks.Add(toRow);
        }
        else {
            toRow.QuantityOnHand += quantity;
            toRow.UpdatedTimestamp = DateTime.UtcNow;
        }

        context.Movements.Add(CreateMovementEntity(itemId, HomeItemMovementType.StockTransfer, quantity, fromLocationId, toLocationId, referenceNumber, createdBy, resolvedTenant));
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        await tx.CommitAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<HomeItemMovementRecord>> ListMovementsForItemAsync(Guid itemId, int take, Guid? tenantId, CancellationToken ct = default)
    {
        var resolvedTenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var q = context.Movements.AsNoTracking().Where(m => m.ItemId == itemId && m.TenantId == resolvedTenant).OrderByDescending(m => m.CreatedTimestamp);
        var list = take > 0 ? await q.Take(take).ToListAsync(ct).ConfigureAwait(false) : await q.ToListAsync(ct).ConfigureAwait(false);
        return list.Select(ToMovementRecord).ToList();
    }

    /// <inheritdoc />
    public async Task SaveAttachmentAsync(HomeAttachmentRecord attachment, Guid? tenantId, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(attachment);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(attachment.EntityType, nameof(attachment.EntityType));
        OperationHelpers.ThrowIf(attachment.EntityId == Guid.Empty, "Attachment entity id is required.");
        OperationHelpers.ThrowIf(attachment.FileId == Guid.Empty, "Attachment file id is required.");
        var resolvedTenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        if (attachment.Id != Guid.Empty) {
            var existing = await context.Attachments.FirstOrDefaultAsync(a => a.Id == attachment.Id && a.TenantId == resolvedTenant, ct).ConfigureAwait(false);
            if (existing != null) {
                CopyToEntity(attachment, existing);
                existing.TenantId = resolvedTenant;
                await context.SaveChangesAsync(ct).ConfigureAwait(false);
                return;
            }
        }

        var entity = new HomeAttachmentEntity();
        CopyToEntity(attachment, entity);
        entity.Id = attachment.Id == Guid.Empty ? LyoGuid.CreateV7() : attachment.Id;
        entity.TenantId = resolvedTenant;
        if (entity.CreatedTimestamp == default)
            entity.CreatedTimestamp = DateTime.UtcNow;

        context.Attachments.Add(entity);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<HomeAttachmentRecord?> GetAttachmentByIdAsync(Guid id, Guid? tenantId, CancellationToken ct = default)
    {
        var resolvedTenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await context.Attachments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id && a.TenantId == resolvedTenant, ct).ConfigureAwait(false);
        return entity == null ? null : ToAttachmentRecord(entity);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<HomeAttachmentRecord>> ListAttachmentsAsync(string entityType, Guid entityId, Guid? tenantId, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(entityType);
        var resolvedTenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var list = await context.Attachments.AsNoTracking()
            .Where(a => a.EntityType == entityType && a.EntityId == entityId && a.TenantId == resolvedTenant)
            .OrderBy(a => a.SortOrder)
            .ThenBy(a => a.CreatedTimestamp)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return list.Select(ToAttachmentRecord).ToList();
    }

    /// <inheritdoc />
    public async Task DeleteAttachmentAsync(Guid id, Guid? tenantId, CancellationToken ct = default)
    {
        var resolvedTenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await context.Attachments.FirstOrDefaultAsync(a => a.Id == id && a.TenantId == resolvedTenant, ct).ConfigureAwait(false);
        if (entity != null) {
            context.Attachments.Remove(entity);
            await context.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    private Guid? ResolveTenant(Guid? tenantId) => TenancyResolver.Resolve(tenantId, _featureTenancy, _entityRefOptions);

    private static async Task RemoveAttachmentsAsync(HomeInventoryDbContext context, string entityType, Guid entityId, Guid? tenantId, CancellationToken ct)
    {
        var rows = await context.Attachments.Where(a => a.EntityType == entityType && a.EntityId == entityId && a.TenantId == tenantId).ToListAsync(ct).ConfigureAwait(false);
        if (rows.Count > 0)
            context.Attachments.RemoveRange(rows);
    }

    private static HomeItemMovementEntity CreateMovementEntity(
        Guid itemId,
        HomeItemMovementType type,
        decimal quantity,
        Guid? fromLocationId,
        Guid? toLocationId,
        string? referenceNumber,
        EntityRef? createdBy,
        Guid? tenantId)
        => new() {
            Id = LyoGuid.CreateV7(),
            ItemId = itemId,
            MovementType = (int)type,
            Quantity = quantity,
            FromLocationId = fromLocationId,
            ToLocationId = toLocationId,
            ReferenceNumber = referenceNumber,
            CreatedByEntityType = createdBy?.EntityType,
            CreatedByEntityId = createdBy?.EntityId,
            TenantId = tenantId,
            CreatedTimestamp = DateTime.UtcNow
        };

    private static void CopyToEntity(HomeItemRecord item, HomeItemEntity e)
    {
        e.OwnerEntityType = item.OwnerEntityType;
        e.OwnerEntityId = item.OwnerEntityId;
        e.CategoryId = item.CategoryId;
        e.ParentItemId = item.ParentItemId;
        e.LocationId = item.LocationId;
        e.Name = item.Name;
        e.Description = item.Description;
        e.Status = (int)item.Status;
        e.Condition = (int)item.Condition;
        e.ProductKeysJson = HomeProductKeyJson.Serialize(item.ProductKeys);
    }

    private static HomeItemRecord ToItemRecord(HomeItemEntity e)
        => new() {
            Id = e.Id,
            OwnerEntityType = e.OwnerEntityType,
            OwnerEntityId = e.OwnerEntityId,
            CategoryId = e.CategoryId,
            ParentItemId = e.ParentItemId,
            LocationId = e.LocationId,
            Name = e.Name,
            Description = e.Description,
            Status = (HomeItemStatus)e.Status,
            Condition = (HomeItemCondition)e.Condition,
            ProductKeys = HomeProductKeyJson.Deserialize(e.ProductKeysJson),
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
            CreatedByEntityType = e.CreatedByEntityType,
            CreatedByEntityId = e.CreatedByEntityId,
            CreatedTimestamp = e.CreatedTimestamp
        };

    private static void CopyToEntity(HomeAttachmentRecord attachment, HomeAttachmentEntity e)
    {
        e.EntityType = attachment.EntityType;
        e.EntityId = attachment.EntityId;
        e.FileId = attachment.FileId;
        e.Role = (int)attachment.Role;
        e.Caption = attachment.Caption;
        e.SortOrder = attachment.SortOrder;
        e.IsPrimary = attachment.IsPrimary;
    }

    private static HomeAttachmentRecord ToAttachmentRecord(HomeAttachmentEntity e)
        => new() {
            Id = e.Id,
            EntityType = e.EntityType,
            EntityId = e.EntityId,
            FileId = e.FileId,
            Role = (HomeAttachmentRole)e.Role,
            Caption = e.Caption,
            SortOrder = e.SortOrder,
            IsPrimary = e.IsPrimary,
            CreatedTimestamp = e.CreatedTimestamp,
            UpdatedTimestamp = e.UpdatedTimestamp
        };
}