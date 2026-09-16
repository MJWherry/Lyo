namespace Lyo.HomeInventory.Models.Request;

/// <summary>Body for nested <c>POST …/Tags</c>.</summary>
public sealed class HomeTagAddReq
{
    /// <summary>Tag display value.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Tag category; defaults to <c>tag</c>.</summary>
    public string TagType { get; set; } = "tag";

    /// <summary>Optional slug.</summary>
    public string? Slug { get; set; }
}
