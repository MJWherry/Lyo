namespace Lyo.Story.Models.Request;

/// <summary>Body for <c>POST /Story/playthrough/{id}/variables</c>. Writes externally owned values, such as game facts, into the playthrough.</summary>
public sealed class StorySetVariablesReq
{
    /// <summary>Variable values by key. Null clears a value.</summary>
    public Dictionary<string, string?> Values { get; set; } = new(StringComparer.Ordinal);
}