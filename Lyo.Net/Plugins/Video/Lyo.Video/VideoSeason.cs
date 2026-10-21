using System.Diagnostics;

namespace Lyo.Video;

/// <summary>A season of a television series. Movies cannot own seasons.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class VideoSeason
{
    public Guid Id { get; set; }

    public Guid TitleId { get; set; }

    public int SeasonNumber { get; set; }

    public string? Name { get; set; }

    public int? Year { get; set; }

    public IReadOnlyList<VideoEpisode> Episodes { get; set; } = [];

    /// <inheritdoc />
    public override string ToString() => $"VideoSeason(Number={SeasonNumber}, Name=\"{Name}\")";
}