using Lyo.Podcast.Enums;

namespace Lyo.Podcast;

/// <summary>Filter for show search. <see cref="FilterIds" /> null means no id filter. An empty list returns no rows.</summary>
public sealed class PodcastShowQuery
{
    public string? TitleContains { get; set; }

    public PodcastShowStatus? Status { get; set; }

    public Guid? GenreId { get; set; }

    public IReadOnlyList<Guid>? FilterIds { get; set; }
}
