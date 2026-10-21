namespace Lyo.Recipe.Models.Request;

/// <summary>Create or update an ingredient catalog row.</summary>
public sealed class IngredientReq
{
    /// <summary>Display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>URL-friendly slug.</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Usual unit when this ingredient is used on a line.</summary>
    public string? DefaultUnit { get; set; }

    /// <summary>Catalog notes (not a Note plugin row).</summary>
    public string? Notes { get; set; }
}