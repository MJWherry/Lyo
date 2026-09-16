using Lyo.EntityReference.Models;
using Lyo.Recipe.Models;

namespace Lyo.Recipe;

/// <summary>Hierarchical grouping for recipes (for example Desserts / Pies).</summary>
[EntityRefLogicalType(RecipeEntityTypes.RecipeCategory)]
public sealed class RecipeCategoryRecord
{
    /// <summary>Row id.</summary>
    public Guid Id { get; set; }

    /// <summary>Parent grouping. Null for a root category.</summary>
    public Guid? ParentId { get; set; }

    /// <summary>Display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>URL-friendly slug.</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Created (UTC).</summary>
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>Last write (UTC).</summary>
    public DateTime? UpdatedTimestamp { get; set; }
}
