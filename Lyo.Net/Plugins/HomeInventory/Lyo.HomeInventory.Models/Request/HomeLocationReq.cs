namespace Lyo.HomeInventory.Models.Request;

/// <summary>Create or update a location (room, closet, or bin).</summary>
public sealed class HomeLocationReq
{
    /// <summary>Parent location. Null for a top-level room.</summary>
    public Guid? ParentLocationId { get; set; }

    /// <summary>Display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Short code printed on labels or scanned.</summary>
    public string? Code { get; set; }

    /// <summary>Optional longer description.</summary>
    public string? Description { get; set; }

    /// <summary>When false, the location is hidden from default lists.</summary>
    public bool IsActive { get; set; } = true;
}
