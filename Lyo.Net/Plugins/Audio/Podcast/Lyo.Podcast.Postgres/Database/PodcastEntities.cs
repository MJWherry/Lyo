using System.Text.Json.Serialization;
using Lyo.Podcast.Enums;
using NpgsqlTypes;

namespace Lyo.Podcast.Postgres.Database;

public sealed class PodcastShowEntity
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Subtitle { get; set; }

    public string Slug { get; set; } = string.Empty;

    public PodcastShowStatus Status { get; set; }

    public string? Language { get; set; }

    public string? CoverImageRef { get; set; }

    public string? Overview { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    public ICollection<PodcastCreditEntity> Credits { get; set; } = [];

    public ICollection<PodcastShowIdentifierEntity> Identifiers { get; set; } = [];

    public ICollection<PodcastSeasonEntity> Seasons { get; set; } = [];

    public ICollection<PodcastShowGenreEntity> GenreLinks { get; set; } = [];
}

public sealed class PodcastSeasonEntity
{
    public Guid Id { get; set; }

    public Guid ShowId { get; set; }

    public int SeasonNumber { get; set; }

    public string? Name { get; set; }

    public int? Year { get; set; }

    public PodcastShowEntity? Show { get; set; }

    public ICollection<PodcastEpisodeEntity> Episodes { get; set; } = [];
}

public sealed class PodcastEpisodeEntity
{
    public Guid Id { get; set; }

    public Guid SeasonId { get; set; }

    public int EpisodeNumber { get; set; }

    public string Title { get; set; } = string.Empty;

    public int? RuntimeSeconds { get; set; }

    public DateOnly? PublishedDate { get; set; }

    public string? AudioFileRef { get; set; }

    public string? Description { get; set; }

    [JsonIgnore]
    public string? Transcript { get; set; }

    [JsonIgnore]
    public NpgsqlTsVector SearchVector { get; set; } = null!;

    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    public PodcastSeasonEntity? Season { get; set; }

    public ICollection<PodcastCreditEntity> Credits { get; set; } = [];

    public ICollection<PodcastEpisodeIdentifierEntity> Identifiers { get; set; } = [];
}

public sealed class PodcastCreditEntity
{
    public Guid Id { get; set; }

    public Guid? ShowId { get; set; }

    public Guid? EpisodeId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string? SortName { get; set; }

    public PodcastCreditRole Role { get; set; }

    public int Ordinal { get; set; }

    public string? PersonRef { get; set; }

    public PodcastShowEntity? Show { get; set; }

    public PodcastEpisodeEntity? Episode { get; set; }
}

public sealed class PodcastShowIdentifierEntity
{
    public Guid Id { get; set; }

    public Guid ShowId { get; set; }

    public string Scheme { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public string NormalizedValue { get; set; } = string.Empty;

    public bool IsPrimary { get; set; }

    public PodcastShowEntity? Show { get; set; }
}

public sealed class PodcastEpisodeIdentifierEntity
{
    public Guid Id { get; set; }

    public Guid EpisodeId { get; set; }

    public string Scheme { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public string NormalizedValue { get; set; } = string.Empty;

    public bool IsPrimary { get; set; }

    public PodcastEpisodeEntity? Episode { get; set; }
}

public sealed class PodcastGenreEntity
{
    public Guid Id { get; set; }

    public Guid? ParentGenreId { get; set; }

    public string Slug { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public PodcastGenreEntity? Parent { get; set; }

    public ICollection<PodcastShowGenreEntity> ShowLinks { get; set; } = [];
}

public sealed class PodcastShowGenreEntity
{
    public Guid ShowId { get; set; }

    public Guid GenreId { get; set; }

    public PodcastShowEntity? Show { get; set; }

    public PodcastGenreEntity? Genre { get; set; }
}
