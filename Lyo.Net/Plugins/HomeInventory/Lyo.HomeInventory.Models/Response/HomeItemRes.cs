using Lyo.EntityReference.Models;
using Lyo.HomeInventory.Models.Enums;

namespace Lyo.HomeInventory.Models.Response;

/// <summary>Household item as returned by the API.</summary>
public sealed class HomeItemRes
{
    /// <summary>Row id.</summary>
    public Guid Id { get; set; }

    /// <summary>Owner (person, account). Null when unassigned.</summary>
    public EntityRef? Owner { get; set; }

    /// <summary>Optional category.</summary>
    public Guid? CategoryId { get; set; }

    /// <summary>Parent for variants or kit components.</summary>
    public Guid? ParentItemId { get; set; }

    /// <summary>Optional room or location this unique item lives in.</summary>
    public Guid? LocationId { get; set; }

    /// <summary>Display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Catalog description.</summary>
    public string? Description { get; set; }

    /// <summary>Lifecycle status.</summary>
    public HomeItemStatus Status { get; set; }

    /// <summary>Physical or commercial condition.</summary>
    public HomeItemCondition Condition { get; set; }

    /// <summary>Identifiers. The same kind may appear more than once.</summary>
    public List<HomeProductKeyEntry> ProductKeys { get; set; } = [];

    /// <summary>Created (UTC).</summary>
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>Last write (UTC).</summary>
    public DateTime? UpdatedTimestamp { get; set; }
}