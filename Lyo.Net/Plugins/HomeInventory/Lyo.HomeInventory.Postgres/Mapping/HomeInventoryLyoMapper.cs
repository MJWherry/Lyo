using Lyo.Api.Mapping;
using Lyo.EntityReference.Models;
using Lyo.HomeInventory.Models;
using Lyo.HomeInventory.Models.Enums;
using Lyo.HomeInventory.Models.Request;
using Lyo.HomeInventory.Models.Response;
using Lyo.HomeInventory.Postgres.Database;

namespace Lyo.HomeInventory.Postgres.Mapping;

/// <summary>Hand-rolled <see cref="ILyoMapper" /> for HomeInventory Req/entity/Res. Explicit property assignments.</summary>
public sealed class HomeInventoryLyoMapper : ILyoMapper
{
    /// <inheritdoc />
    public TResult Map<TResult>(object source)
        => source switch {
            HomeCategoryReq req when typeof(TResult) == typeof(HomeCategoryEntity) => (TResult)(object)ReqToNew(req),
            HomeLocationReq req when typeof(TResult) == typeof(HomeLocationEntity) => (TResult)(object)ReqToNew(req),
            HomeItemReq req when typeof(TResult) == typeof(HomeItemEntity) => (TResult)(object)ReqToNew(req),
            HomeAttachmentReq req when typeof(TResult) == typeof(HomeAttachmentEntity) => (TResult)(object)ReqToNew(req),
            HomeItemStockReq req when typeof(TResult) == typeof(HomeItemStockEntity) => (TResult)(object)ReqToNew(req),
            HomeCategoryEntity e when typeof(TResult) == typeof(HomeCategoryRes) => (TResult)(object)ToRes(e),
            HomeLocationEntity e when typeof(TResult) == typeof(HomeLocationRes) => (TResult)(object)ToRes(e),
            HomeItemEntity e when typeof(TResult) == typeof(HomeItemRes) => (TResult)(object)ToRes(e),
            HomeAttachmentEntity e when typeof(TResult) == typeof(HomeAttachmentRes) => (TResult)(object)ToRes(e),
            HomeItemMovementEntity e when typeof(TResult) == typeof(HomeItemMovementRes) => (TResult)(object)ToRes(e),
            HomeItemStockEntity e when typeof(TResult) == typeof(HomeItemStockRes) => (TResult)(object)ToRes(e),
            HomeItemStockRecord r when typeof(TResult) == typeof(HomeItemStockRes) => (TResult)(object)ToRes(r),
            var _ => throw Unmapped(source.GetType(), typeof(TResult))
        };

    /// <inheritdoc />
    public void Map<TSource, TDest>(TSource source, TDest destination)
    {
        switch (source, destination) {
            case (HomeCategoryReq req, HomeCategoryEntity e):
                Apply(req, e);
                break;
            case (HomeLocationReq req, HomeLocationEntity e):
                Apply(req, e);
                break;
            case (HomeItemReq req, HomeItemEntity e):
                Apply(req, e);
                break;
            case (HomeAttachmentReq req, HomeAttachmentEntity e):
                Apply(req, e);
                break;
            case (HomeItemStockReq req, HomeItemStockEntity e):
                Apply(req, e);
                break;
            default:
                throw Unmapped(typeof(TSource), typeof(TDest));
        }
    }

    private static InvalidOperationException Unmapped(Type source, Type dest) => new($"No mapping configured from {source.Name} to {dest.Name}.");

    private static DateTime UtcNow() => DateTime.UtcNow;

    internal static HomeCategoryEntity ReqToNew(HomeCategoryReq r)
    {
        var dest = new HomeCategoryEntity { CreatedTimestamp = UtcNow() };
        Apply(r, dest);
        return dest;
    }

    internal static HomeLocationEntity ReqToNew(HomeLocationReq r)
    {
        var dest = new HomeLocationEntity { CreatedTimestamp = UtcNow() };
        Apply(r, dest);
        return dest;
    }

    internal static HomeItemEntity ReqToNew(HomeItemReq r)
    {
        var dest = new HomeItemEntity { CreatedTimestamp = UtcNow() };
        Apply(r, dest);
        return dest;
    }

    internal static HomeAttachmentEntity ReqToNew(HomeAttachmentReq r)
    {
        var dest = new HomeAttachmentEntity { CreatedTimestamp = UtcNow() };
        Apply(r, dest);
        return dest;
    }

    internal static HomeItemStockEntity ReqToNew(HomeItemStockReq r)
    {
        var dest = new HomeItemStockEntity { UpdatedTimestamp = UtcNow() };
        Apply(r, dest);
        return dest;
    }

    internal static HomeCategoryRes ToRes(HomeCategoryEntity e)
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

    internal static HomeLocationRes ToRes(HomeLocationEntity e)
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

    internal static HomeItemRes ToRes(HomeItemEntity e)
        => new() {
            Id = e.Id,
            Owner = ToOwner(e.OwnerEntityType, e.OwnerEntityId),
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

    internal static HomeAttachmentRes ToRes(HomeAttachmentEntity e)
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

    internal static HomeItemMovementRes ToRes(HomeItemMovementEntity e)
        => new() {
            Id = e.Id,
            ItemId = e.ItemId,
            MovementType = (HomeItemMovementType)e.MovementType,
            Quantity = e.Quantity,
            FromLocationId = e.FromLocationId,
            ToLocationId = e.ToLocationId,
            ReferenceNumber = e.ReferenceNumber,
            CreatedBy = ToOwner(e.CreatedByEntityType, e.CreatedByEntityId),
            CreatedTimestamp = e.CreatedTimestamp
        };

    internal static HomeItemStockRes ToRes(HomeItemStockEntity e)
        => new() {
            ItemId = e.ItemId,
            LocationId = e.LocationId,
            QuantityOnHand = e.QuantityOnHand,
            QuantityReserved = e.QuantityReserved,
            ReorderPoint = e.ReorderPoint,
            UpdatedTimestamp = e.UpdatedTimestamp
        };

    internal static HomeItemStockRes ToRes(HomeItemStockRecord r)
        => new() {
            ItemId = r.ItemId,
            LocationId = r.LocationId,
            QuantityOnHand = r.QuantityOnHand,
            QuantityReserved = r.QuantityReserved,
            ReorderPoint = r.ReorderPoint,
            UpdatedTimestamp = r.UpdatedTimestamp
        };

    private static void Apply(HomeCategoryReq r, HomeCategoryEntity e)
    {
        e.ParentCategoryId = r.ParentCategoryId;
        e.Name = r.Name;
        e.Slug = r.Slug;
        e.Description = r.Description;
        e.SortOrder = r.SortOrder;
    }

    private static void Apply(HomeLocationReq r, HomeLocationEntity e)
    {
        e.ParentLocationId = r.ParentLocationId;
        e.Name = r.Name;
        e.Code = r.Code;
        e.Description = r.Description;
        e.IsActive = r.IsActive;
    }

    private static void Apply(HomeItemReq r, HomeItemEntity e)
    {
        e.OwnerEntityType = r.Owner?.EntityType;
        e.OwnerEntityId = r.Owner?.EntityId;
        e.CategoryId = r.CategoryId;
        e.ParentItemId = r.ParentItemId;
        e.LocationId = r.LocationId;
        e.Name = r.Name;
        e.Description = r.Description;
        e.Status = (int)r.Status;
        e.Condition = (int)r.Condition;
        e.ProductKeysJson = HomeProductKeyJson.Serialize(r.ProductKeys);
    }

    private static void Apply(HomeAttachmentReq r, HomeAttachmentEntity e)
    {
        e.EntityType = r.EntityType;
        e.EntityId = r.EntityId;
        e.FileId = r.FileId;
        e.Role = (int)r.Role;
        e.Caption = r.Caption;
        e.SortOrder = r.SortOrder;
        e.IsPrimary = r.IsPrimary;
    }

    private static void Apply(HomeItemStockReq r, HomeItemStockEntity e)
    {
        e.ItemId = r.ItemId;
        e.LocationId = r.LocationId;
        e.QuantityOnHand = r.QuantityOnHand;
        e.QuantityReserved = r.QuantityReserved;
        e.ReorderPoint = r.ReorderPoint;
    }

    private static EntityRef? ToOwner(string? type, string? id) => string.IsNullOrWhiteSpace(type) || string.IsNullOrWhiteSpace(id) ? null : new EntityRef(type, id);
}