namespace Lyo.Music;

/// <summary>Full-text query over track lyrics.</summary>
public sealed class MusicLyricsQuery
{
    /// <summary>Plain-text query passed to <c>plainto_tsquery('english', ...)</c>.</summary>
    public string? FullText { get; set; }
}
