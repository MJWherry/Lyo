namespace Lyo.Recipe.Models.Request;

/// <summary>Body for nested <c>POST …/Notes</c>.</summary>
public sealed class RecipeNoteCreateReq
{
    /// <summary>Note text.</summary>
    public string Content { get; set; } = string.Empty;
}
