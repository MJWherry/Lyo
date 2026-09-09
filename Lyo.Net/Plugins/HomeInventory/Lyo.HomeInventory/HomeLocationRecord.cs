namespace Lyo.HomeInventory;

/// <summary>Place the item lives: room, closet, shelf, or nested zone (e.g. Office / Desk drawer).</summary>
public sealed class HomeLocationRecord
{
    public Guid Id { get; set; }

    public Guid? ParentLocationId { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Short code printed on labels or scanned.</summary>
    public string? Code { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }
}