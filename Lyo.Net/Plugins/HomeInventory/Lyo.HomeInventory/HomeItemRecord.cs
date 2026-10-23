using Lyo.Common.Core.Extensions;
using Lyo.EntityReference.Models;
using Lyo.HomeInventory.Models;
using Lyo.HomeInventory.Models.Enums;

namespace Lyo.HomeInventory;

/// <summary>
/// A household item: device, appliance, major purchase, and similar. Identifiers live on <see cref="ProductKeys" /> (SKU, serial, Wi-Fi MAC, …). Free-text comments live on
/// <c>INoteStore</c>, not this row.
/// </summary>
[EntityRefLogicalType(HomeInventoryEntityTypes.Item)]
public sealed class HomeItemRecord
{
    /// <summary>Row id.</summary>
    public Guid Id { get; set; }

    /// <summary>owner (e.g. Person, Account) using EntityRef components. Present only when supplied.</summary>
    public string? OwnerEntityType { get; set; }

    /// <summary>Owner entity id.</summary>
    public string? OwnerEntityId { get; set; }

    /// <summary>Optional category.</summary>
    public Guid? CategoryId { get; set; }

    /// <summary>parent for variants or kit components. Present only when supplied.</summary>
    public Guid? ParentItemId { get; set; }

    /// <summary>Optional room or location this unique item lives in. Independent of stock rows.</summary>
    public Guid? LocationId { get; set; }

    /// <summary>Display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Catalog description.</summary>
    public string? Description { get; set; }

    /// <summary>Lifecycle status.</summary>
    public HomeItemStatus Status { get; set; } = HomeItemStatus.Active;

    /// <summary>Physical or commercial condition.</summary>
    public HomeItemCondition Condition { get; set; } = HomeItemCondition.Unknown;

    /// <summary>Identifiers (SKU, serial, Wi-Fi MAC, …). The same <see cref="HomeProductKeyEntry.Kind" /> may appear more than once. Each row may carry a note for that pair.</summary>
    public List<HomeProductKeyEntry> ProductKeys { get; set; } = [];

    /// <summary>Created (UTC).</summary>
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>Last write (UTC).</summary>
    public DateTime? UpdatedTimestamp { get; set; }

    /// <summary>Owner as an <see cref="EntityRef" /> when both type and id are set.</summary>
    public EntityRef? Owner => OwnerEntityType.IsNullOrWhitespace() || OwnerEntityId.IsNullOrWhitespace() ? null : new EntityRef(OwnerEntityType, OwnerEntityId);
}