using Lyo.HomeInventory.Models.Enums;

namespace Lyo.HomeInventory.Models.Request;

/// <summary>Link a FileStorage file to an inventory subject.</summary>
public sealed class HomeAttachmentReq
{
    /// <summary>Subject type (<see cref="HomeInventoryEntityTypes" />).</summary>
    public string EntityType { get; set; } = HomeInventoryEntityTypes.Item;

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
}