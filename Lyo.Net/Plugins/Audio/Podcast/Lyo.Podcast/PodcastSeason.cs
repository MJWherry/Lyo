using System.Diagnostics;

namespace Lyo.Podcast;

/// <summary>A season of a show. Season number is unique per show.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class PodcastSeason
{
    public Guid Id { get; set; }

    public Guid ShowId { get; set; }

    public int SeasonNumber { get; set; }

    public string? Name { get; set; }

    public int? Year { get; set; }

    public IReadOnlyList<PodcastEpisode> Episodes { get; set; } = [];

    /// <inheritdoc />
    public override string ToString() => $"PodcastSeason(Number={SeasonNumber})";
}
