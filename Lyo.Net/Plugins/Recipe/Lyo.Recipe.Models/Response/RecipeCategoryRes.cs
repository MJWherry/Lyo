namespace Lyo.Recipe.Models.Response;

/// <summary>Category as returned by the API.</summary>
public sealed class RecipeCategoryRes
{
    /// <summary>Row id.</summary>
    public Guid Id { get; set; }

    /// <summary>Parent grouping.</summary>
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