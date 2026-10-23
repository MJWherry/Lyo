using System.Diagnostics;

namespace Lyo.Video;

/// <summary>An optional franchise that groups titles. Deleting it keeps the titles.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class VideoCollection
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    /// <inheritdoc />
    public override string ToString() => $"VideoCollection(Id={Id}, Title=\"{Title}\")";
}