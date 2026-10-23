using System.Diagnostics;

namespace Lyo.Audiobook;

/// <summary>A series of audiobook works. Deleting it clears <see cref="Audiobook.SeriesId" /> and <see cref="Audiobook.SeriesIndex" />. Works stay.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class AudiobookSeries
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    /// <inheritdoc />
    public override string ToString() => $"AudiobookSeries(Title=\"{Title}\")";
}