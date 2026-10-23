using System.Text.Json.Serialization;
using Lyo.Video.Enums;
using NpgsqlTypes;

namespace Lyo.Video.Postgres.Database;

public sealed class VideoTitleEntity
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public VideoTitleKind Kind { get; set; }

    public VideoSeriesStatus Status { get; set; }

    public int? Year { get; set; }

    public string? Language { get; set; }

    public int? RuntimeSeconds { get; set; }

    public string? CoverImageRef { get; set; }

    public string? VideoFileRef { get; set; }

    public string? Overview { get; set; }

    [JsonIgnore]
    public string? Transcript { get; set; }

    [JsonIgnore]
    public NpgsqlTsVector SearchVector { get; set; } = null!;

    public Guid? CollectionId { get; set; }

    public VideoCollectionEntity? Collection { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    public ICollection<VideoCreditEntity> Credits { get; set; } = [];

    public ICollection<VideoTitleIdentifierEntity> Identifiers { get; set; } = [];

    public ICollection<VideoSeasonEntity> Seasons { get; set; } = [];

    public ICollection<VideoTitleGenreEntity> GenreLinks { get; set; } = [];

    public ICollection<VideoAlternateTitleEntity> AlternateTitles { get; set; } = [];

    public ICollection<VideoCutEntity> Cuts { get; set; } = [];

    public VideoYoutubeLinkEntity? Youtube { get; set; }
}

public sealed class VideoSeasonEntity
{
    public Guid Id { get; set; }

    public Guid TitleId { get; set; }

    public int SeasonNumber { get; set; }

    public string? Name { get; set; }

    public int? Year { get; set; }

    public VideoTitleEntity? Title { get; set; }

    public ICollection<VideoEpisodeEntity> Episodes { get; set; } = [];
}

public sealed class VideoEpisodeEntity
{
    public Guid Id { get; set; }

    public Guid SeasonId { get; set; }

    public int EpisodeNumber { get; set; }

    public string Title { get; set; } = string.Empty;

    public int? RuntimeSeconds { get; set; }

    public DateOnly? AirDate { get; set; }

    public string? VideoFileRef { get; set; }

    public string? StillImageRef { get; set; }

    [JsonIgnore]
    public string? Transcript { get; set; }

    [JsonIgnore]
    public NpgsqlTsVector SearchVector { get; set; } = null!;

    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    public VideoSeasonEntity? Season { get; set; }

    public ICollection<VideoCreditEntity> Credits { get; set; } = [];

    public ICollection<VideoEpisodeIdentifierEntity> Identifiers { get; set; } = [];

    public VideoYoutubeLinkEntity? Youtube { get; set; }
}

public sealed class VideoCreditEntity
{
    public Guid Id { get; set; }

    public Guid? TitleId { get; set; }

    public Guid? EpisodeId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string? SortName { get; set; }

    public VideoCreditRole Role { get; set; }

    public int Ordinal { get; set; }

    public string? CharacterName { get; set; }

    public string? PersonRef { get; set; }

    public VideoTitleEntity? Title { get; set; }

    public VideoEpisodeEntity? Episode { get; set; }
}

public sealed class VideoTitleIdentifierEntity
{
    public Guid Id { get; set; }

    public Guid TitleId { get; set; }

    public string Scheme { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public string NormalizedValue { get; set; } = string.Empty;

    public bool IsPrimary { get; set; }

    public VideoTitleEntity? Title { get; set; }
}

public sealed class VideoEpisodeIdentifierEntity
{
    public Guid Id { get; set; }

    public Guid EpisodeId { get; set; }

    public string Scheme { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public string NormalizedValue { get; set; } = string.Empty;

    public bool IsPrimary { get; set; }

    public VideoEpisodeEntity? Episode { get; set; }
}

public sealed class VideoYoutubeLinkEntity
{
    public Guid Id { get; set; }

    public Guid? TitleId { get; set; }

    public Guid? EpisodeId { get; set; }

    public string VideoId { get; set; } = string.Empty;

    public string ChannelId { get; set; } = string.Empty;

    public string? PlaylistId { get; set; }

    public VideoTitleEntity? Title { get; set; }

    public VideoEpisodeEntity? Episode { get; set; }
}