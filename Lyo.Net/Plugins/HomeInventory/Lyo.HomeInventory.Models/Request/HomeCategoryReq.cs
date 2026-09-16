namespace Lyo.HomeInventory.Models.Request;

/// <summary>Create or update a category.</summary>
public sealed class HomeCategoryReq
{
    /// <summary>Parent grouping. Null for a root category.</summary>
    public Guid? ParentCategoryId { get; set; }

    /// <summary>Display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>URL-friendly slug.</summary>
    public string? Slug { get; set; }

    /// <summary>Optional longer description.</summary>
    public string? Description { get; set; }

    /// <summary>Sort among siblings.</summary>
    public int SortOrder { get; set; }
}
