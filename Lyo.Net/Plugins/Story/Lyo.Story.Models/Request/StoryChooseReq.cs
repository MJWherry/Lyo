namespace Lyo.Story.Models.Request;

/// <summary>Body for <c>POST /Story/playthrough/{id}/choose</c>.</summary>
public sealed class StoryChooseReq
{
    /// <summary>Choice key on the current node.</summary>
    public string ChoiceKey { get; set; } = string.Empty;
}
