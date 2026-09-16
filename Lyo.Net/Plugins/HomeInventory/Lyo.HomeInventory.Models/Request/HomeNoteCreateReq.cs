namespace Lyo.HomeInventory.Models.Request;

/// <summary>Body for nested <c>POST …/Notes</c>.</summary>
public sealed class HomeNoteCreateReq
{
    /// <summary>Note text.</summary>
    public string Content { get; set; } = string.Empty;
}
