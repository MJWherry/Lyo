namespace Lyo.Recipe.Models.Request;

/// <summary>Create or update a recipe category.</summary>
public sealed class RecipeCategoryReq
{
    /// <summary>Parent grouping. Null for a root category.</summary>
    public Guid? ParentId { get; set; }

    /// <summary>Display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>URL-friendly slug.</summary>
    public string Slug { get; set; } = string.Empty;
}
