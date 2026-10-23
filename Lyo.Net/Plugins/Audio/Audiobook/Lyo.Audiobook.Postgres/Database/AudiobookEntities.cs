using System.Text.Json.Serialization;
using Lyo.Audiobook.Enums;
using NpgsqlTypes;

namespace Lyo.Audiobook.Postgres.Database;

public sealed class AudiobookWorkEntity
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Subtitle { get; set; }

    public string Slug { get; set; } = string.Empty;

    public string? Language { get; set; }

    public int? Year { get; set; }

    public int? RuntimeSeconds { get; set; }

    public string? CoverImageRef { get; set; }

    public string? AudioFileRef { get; set; }

    public string? Overview { get; set; }

    public Guid? SeriesId { get; set; }

    public decimal? SeriesIndex { get; set; }

    [JsonIgnore]
    public string? Transcript { get; set; }

    [JsonIgnore]
    public NpgsqlTsVector SearchVector { get; set; } = null!;

    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    public AudiobookSeriesEntity? Series { get; set; }

    public ICollection<AudiobookCreditEntity> Credits { get; set; } = [];

    public ICollection<AudiobookIdentifierEntity> Identifiers { get; set; } = [];

    public ICollection<AudiobookChapterEntity> Chapters { get; set; } = [];

    public ICollection<AudiobookWorkGenreEntity> GenreLinks { get; set; } = [];
}

public sealed class AudiobookChapterEntity
{
    public Guid Id { get; set; }

    public Guid WorkId { get; set; }

    public int Number { get; set; }

    public string Title { get; set; } = string.Empty;

    public int? StartSeconds { get; set; }

    public int? DurationSeconds { get; set; }

    public string? AudioFileRef { get; set; }

    [JsonIgnore]
    public string? Transcript { get; set; }

    [JsonIgnore]
    public NpgsqlTsVector SearchVector { get; set; } = null!;

    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    public AudiobookWorkEntity? Work { get; set; }
}

public sealed class AudiobookCreditEntity
{
    public Guid Id { get; set; }

    public Guid WorkId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string? SortName { get; set; }

    public AudiobookCreditRole Role { get; set; }

    public int Ordinal { get; set; }

    public string? PersonRef { get; set; }

    public AudiobookWorkEntity? Work { get; set; }
}

public sealed class AudiobookIdentifierEntity
{
    public Guid Id { get; set; }

    public Guid WorkId { get; set; }

    public string Scheme { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public string NormalizedValue { get; set; } = string.Empty;

    public bool IsPrimary { get; set; }

    public AudiobookWorkEntity? Work { get; set; }
}

public sealed class AudiobookSeriesEntity
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public ICollection<AudiobookWorkEntity> Works { get; set; } = [];
}

public sealed class AudiobookGenreEntity
{
    public Guid Id { get; set; }

    public Guid? ParentGenreId { get; set; }

    public string Slug { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public AudiobookGenreEntity? Parent { get; set; }

    public ICollection<AudiobookWorkGenreEntity> WorkLinks { get; set; } = [];
}

public sealed class AudiobookWorkGenreEntity
{
    public Guid WorkId { get; set; }

    public Guid GenreId { get; set; }

    public AudiobookWorkEntity? Work { get; set; }

    public AudiobookGenreEntity? Genre { get; set; }
}