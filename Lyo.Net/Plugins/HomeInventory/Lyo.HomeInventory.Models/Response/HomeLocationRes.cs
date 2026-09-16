namespace Lyo.HomeInventory.Models.Response;

/// <summary>Location as returned by the API.</summary>
public sealed class HomeLocationRes
{
    /// <summary>Row id.</summary>
    public Guid Id { get; set; }

    /// <summary>Parent location.</summary>
    public Guid? ParentLocationId { get; set; }

    /// <summary>Display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Short code printed on labels or scanned.</summary>
    public string? Code { get; set; }

    /// <summary>Optional longer description.</summary>
    public string? Description { get; set; }

    /// <summary>When false, the location is hidden from default lists.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Created (UTC).</summary>
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>Last write (UTC).</summary>
    public DateTime? UpdatedTimestamp { get; set; }
}
