using Lyo.EntityReference.Models;
using Lyo.HomeInventory.Models.Enums;

namespace Lyo.HomeInventory;

/// <summary>FileStorage file linked to an inventory subject (item, location, or category).</summary>
[EntityRefLogicalType(Models.HomeInventoryEntityTypes.Attachment)]
public sealed class HomeAttachmentRecord
{
    /// <summary>Row id.</summary>
    public Guid Id { get; set; }

    /// <summary>Subject type (<see cref="Models.HomeInventoryEntityTypes" />).</summary>
    public string EntityType { get; set; } = Models.HomeInventoryEntityTypes.Item;

    /// <summary>Subject id.</summary>
    public Guid EntityId { get; set; }

    /// <summary>FileStorage file id.</summary>
    public Guid FileId { get; set; }

    /// <summary>How the file relates to the subject.</summary>
    public HomeAttachmentRole Role { get; set; } = HomeAttachmentRole.Document;

    /// <summary>Optional caption.</summary>
    public string? Caption { get; set; }

    /// <summary>Sort among siblings.</summary>
    public int SortOrder { get; set; }

    /// <summary>When true, this photo is the primary thumbnail.</summary>
    public bool IsPrimary { get; set; }

    /// <summary>Created (UTC).</summary>
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>Last write (UTC).</summary>
    public DateTime? UpdatedTimestamp { get; set; }
}
