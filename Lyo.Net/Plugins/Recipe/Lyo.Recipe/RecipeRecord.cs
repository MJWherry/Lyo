using Lyo.EntityReference.Models;
using Lyo.Recipe.Models;
using Lyo.Recipe.Models.Enums;

namespace Lyo.Recipe;

/// <summary>A household recipe. Ingredient lines and steps are stored separately.</summary>
[EntityRefLogicalType(RecipeEntityTypes.Recipe)]
public sealed class RecipeRecord
{
    /// <summary>Row id.</summary>
    public Guid Id { get; set; }

    /// <summary>Display title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>URL-friendly slug.</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Longer description (not a Note plugin row).</summary>
    public string? Description { get; set; }

    /// <summary>How many people the listed quantities feed.</summary>
    public int? Servings { get; set; }

    /// <summary>Active prep time in minutes.</summary>
    public int? PrepMinutes { get; set; }

    /// <summary>Cook or bake time in minutes.</summary>
    public int? CookMinutes { get; set; }

    /// <summary>How involved the cook is.</summary>
    public RecipeDifficulty Difficulty { get; set; }

    /// <summary>Lifecycle status.</summary>
    public RecipeStatus Status { get; set; } = RecipeStatus.Draft;

    /// <summary>Book, site, or person the recipe came from.</summary>
    public string? SourceName { get; set; }

    /// <summary>Optional URL for the source.</summary>
    public string? SourceUrl { get; set; }

    /// <summary>Free-text yield when servings are not enough.</summary>
    public string? YieldLabel { get; set; }

    /// <summary>Optional category.</summary>
    public Guid? CategoryId { get; set; }

    /// <summary>Created (UTC).</summary>
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>Last write (UTC).</summary>
    public DateTime? UpdatedTimestamp { get; set; }
}