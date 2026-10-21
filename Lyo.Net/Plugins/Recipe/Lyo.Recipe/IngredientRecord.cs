using Lyo.EntityReference.Models;
using Lyo.Recipe.Models;

namespace Lyo.Recipe;

/// <summary>Reusable ingredient catalog row. Recipe lines may link here or store free text.</summary>
[EntityRefLogicalType(RecipeEntityTypes.Ingredient)]
public sealed class IngredientRecord
{
    /// <summary>Row id.</summary>
    public Guid Id { get; set; }

    /// <summary>Display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>URL-friendly slug.</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Usual unit when this ingredient is used on a line.</summary>
    public string? DefaultUnit { get; set; }

    /// <summary>Catalog notes (not a Note plugin row).</summary>
    public string? Notes { get; set; }

    /// <summary>Created (UTC).</summary>
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>Last write (UTC).</summary>
    public DateTime? UpdatedTimestamp { get; set; }
}