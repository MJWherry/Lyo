namespace Lyo.HomeInventory.Models.Response;

/// <summary>Category as returned by the API.</summary>
public sealed class HomeCategoryRes
{
    /// <summary>Row id.</summary>
    public Guid Id { get; set; }

    /// <summary>Parent grouping.</summary>
    public Guid? ParentCategoryId { get; set; }

    /// <summary>Display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>URL-friendly slug.</summary>
    public string? Slug { get; set; }

    /// <summary>Optional longer description.</summary>
    public string? Description { get; set; }

    /// <summary>Sort among siblings.</summary>
    public int SortOrder { get; set; }

    /// <summary>Created (UTC).</summary>
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>Last write (UTC).</summary>
    public DateTime? UpdatedTimestamp { get; set; }
}
