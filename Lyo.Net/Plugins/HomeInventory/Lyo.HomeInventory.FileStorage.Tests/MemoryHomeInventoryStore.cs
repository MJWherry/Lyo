using Lyo.EntityReference.Models;
using Lyo.HomeInventory;
using Lyo.HomeInventory.Models.Enums;
using Lyo.Common.Core.Identifiers;

namespace Lyo.HomeInventory.FileStorage.Tests;

internal sealed class MemoryHomeInventoryStore : IHomeInventoryStore
{
    public List<HomeAttachmentRecord> Attachments { get; } = [];

    public Task SaveAttachmentAsync(HomeAttachmentRecord attachment, Guid? tenantId, CancellationToken ct = default)
    {
        if (attachment.Id == Guid.Empty)
            attachment.Id = LyoGuid.CreateV7();
        if (attachment.CreatedTimestamp == default)
            attachment.CreatedTimestamp = DateTime.UtcNow;
        var existing = Attachments.FindIndex(a => a.Id == attachment.Id);
        if (existing >= 0)
            Attachments[existing] = attachment;
        else
            Attachments.Add(attachment);
        return Task.CompletedTask;
    }

    public Task<HomeAttachmentRecord?> GetAttachmentByIdAsync(Guid id, Guid? tenantId, CancellationToken ct = default)
        => Task.FromResult(Attachments.FirstOrDefault(a => a.Id == id));

    public Task<IReadOnlyList<HomeAttachmentRecord>> ListAttachmentsAsync(string entityType, Guid entityId, Guid? tenantId, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<HomeAttachmentRecord>>(Attachments.Where(a => a.EntityType == entityType && a.EntityId == entityId).ToList());

    public Task DeleteAttachmentAsync(Guid id, Guid? tenantId, CancellationToken ct = default)
    {
        Attachments.RemoveAll(a => a.Id == id);
        return Task.CompletedTask;
    }

    public Task SaveItemAsync(HomeItemRecord item, Guid? tenantId, CancellationToken ct = default) => throw new NotSupportedException();

    public Task<HomeItemRecord?> GetItemByIdAsync(Guid id, Guid? tenantId, CancellationToken ct = default) => throw new NotSupportedException();

    public Task DeleteItemAsync(Guid id, Guid? tenantId, CancellationToken ct = default) => throw new NotSupportedException();

    public Task SaveCategoryAsync(HomeCategoryRecord category, Guid? tenantId, CancellationToken ct = default) => throw new NotSupportedException();

    public Task<HomeCategoryRecord?> GetCategoryByIdAsync(Guid id, Guid? tenantId, CancellationToken ct = default) => throw new NotSupportedException();

    public Task<IReadOnlyList<HomeCategoryRecord>> ListCategoriesAsync(Guid? tenantId, CancellationToken ct = default) => throw new NotSupportedException();

    public Task DeleteCategoryAsync(Guid id, Guid? tenantId, CancellationToken ct = default) => throw new NotSupportedException();

    public Task SaveLocationAsync(HomeLocationRecord location, Guid? tenantId, CancellationToken ct = default) => throw new NotSupportedException();

    public Task<HomeLocationRecord?> GetLocationByIdAsync(Guid id, Guid? tenantId, CancellationToken ct = default) => throw new NotSupportedException();

    public Task<IReadOnlyList<HomeLocationRecord>> ListLocationsAsync(bool activeOnly, Guid? tenantId, CancellationToken ct = default) => throw new NotSupportedException();

    public Task DeleteLocationAsync(Guid id, Guid? tenantId, CancellationToken ct = default) => throw new NotSupportedException();

    public Task UpsertStockAsync(Guid itemId, Guid locationId, decimal quantityOnHand, decimal quantityReserved, decimal? reorderPoint, Guid? tenantId, CancellationToken ct = default)
        => throw new NotSupportedException();

    public Task<HomeItemStockRecord?> GetStockAsync(Guid itemId, Guid locationId, Guid? tenantId, CancellationToken ct = default) => throw new NotSupportedException();

    public Task<IReadOnlyList<HomeItemStockRecord>> GetStockForItemAsync(Guid itemId, Guid? tenantId, CancellationToken ct = default) => throw new NotSupportedException();

    public Task AdjustStockAsync(
        Guid itemId, Guid locationId, decimal quantityDelta, HomeItemMovementType movementType, string? referenceNumber, EntityRef? createdBy, Guid? tenantId,
        CancellationToken ct = default)
        => throw new NotSupportedException();

    public Task TransferStockAsync(
        Guid itemId, Guid fromLocationId, Guid toLocationId, decimal quantity, string? referenceNumber, EntityRef? createdBy, Guid? tenantId, CancellationToken ct = default)
        => throw new NotSupportedException();

    public Task<IReadOnlyList<HomeItemMovementRecord>> ListMovementsForItemAsync(Guid itemId, int take, Guid? tenantId, CancellationToken ct = default)
        => throw new NotSupportedException();
}
