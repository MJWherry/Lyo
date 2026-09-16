namespace Lyo.Music.Postgres.Database;

public sealed class MusicGenreEntity
{
    public Guid Id { get; set; }

    public Guid? ParentGenreId { get; set; }

    public string Slug { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public MusicGenreEntity? Parent { get; set; }

    public ICollection<MusicReleaseGenreEntity> ReleaseLinks { get; set; } = [];
}

public sealed class MusicFormatEntity
{
    public Guid Id { get; set; }

    public string Slug { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public ICollection<MusicReleaseEntity> Releases { get; set; } = [];
}

public sealed class MusicReleaseGenreEntity
{
    public Guid ReleaseId { get; set; }

    public Guid GenreId { get; set; }

    public MusicReleaseEntity? Release { get; set; }

    public MusicGenreEntity? Genre { get; set; }
}
