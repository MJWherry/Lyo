using Lyo.HomeInventory.Models.Enums;

namespace Lyo.HomeInventory.Models.Response;

/// <summary>FileStorage file linked to an inventory subject.</summary>
public sealed class HomeAttachmentRes
{
    /// <summary>Row id.</summary>
    public Guid Id { get; set; }

    /// <summary>Subject type (<see cref="HomeInventoryEntityTypes" />).</summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>Subject id.</summary>
    public Guid EntityId { get; set; }

    /// <summary>FileStorage file id.</summary>
    public Guid FileId { get; set; }

    /// <summary>How the file relates to the subject.</summary>
    public HomeAttachmentRole Role { get; set; }

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