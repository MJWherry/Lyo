namespace Lyo.Recipe.Models.Response;

/// <summary>Ingredient catalog row as returned by the API.</summary>
public sealed class IngredientRes
{
    /// <summary>Row id.</summary>
    public Guid Id { get; set; }

    /// <summary>Display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>URL-friendly slug.</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Usual unit when this ingredient is used on a line.</summary>
    public string? DefaultUnit { get; set; }

    /// <summary>Catalog notes.</summary>
    public string? Notes { get; set; }

    /// <summary>Created (UTC).</summary>
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>Last write (UTC).</summary>
    public DateTime? UpdatedTimestamp { get; set; }
}