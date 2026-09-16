using Lyo.EntityReference.Models;
using Lyo.HomeInventory.Models.Enums;

namespace Lyo.HomeInventory.Models.Request;

/// <summary>Create or update a household item.</summary>
public sealed class HomeItemReq
{
    /// <summary>Owner (person, account). Omit when unassigned.</summary>
    public EntityRef? Owner { get; set; }

    /// <summary>Optional category.</summary>
    public Guid? CategoryId { get; set; }

    /// <summary>Parent for variants or kit components.</summary>
    public Guid? ParentItemId { get; set; }

    /// <summary>Optional room or location this unique item lives in. Independent of stock rows.</summary>
    public Guid? LocationId { get; set; }

    /// <summary>Display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Catalog description (not a Note plugin row).</summary>
    public string? Description { get; set; }

    /// <summary>Lifecycle status.</summary>
    public HomeItemStatus Status { get; set; } = HomeItemStatus.Active;

    /// <summary>Physical or commercial condition.</summary>
    public HomeItemCondition Condition { get; set; } = HomeItemCondition.Unknown;

    /// <summary>
    /// Identifiers (SKU, serial, Wi-Fi MAC, …). The same <see cref="HomeProductKeyEntry.Kind"/> may appear more than once. Each row may carry a note for that
    /// pair.
    /// </summary>
    public List<HomeProductKeyEntry> ProductKeys { get; set; } = [];
}
