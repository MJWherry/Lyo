namespace Lyo.Story.Models.Request;

/// <summary>Body for <c>POST /Story/playthrough/start</c>.</summary>
public sealed class StoryStartPlaythroughReq
{
    /// <summary>Story to start.</summary>
    public Guid StoryId { get; set; }

    /// <summary>Optional opaque player handle. Not a People id.</summary>
    public string? PlayerRef { get; set; }
}
