using Lyo.Video.Enums;

namespace Lyo.Video.Postgres.Database;

public sealed class VideoGenreEntity
{
    public Guid Id { get; set; }

    public Guid? ParentGenreId { get; set; }

    public string Slug { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public VideoGenreEntity? Parent { get; set; }

    public ICollection<VideoTitleGenreEntity> TitleLinks { get; set; } = [];
}

public sealed class VideoTitleGenreEntity
{
    public Guid TitleId { get; set; }

    public Guid GenreId { get; set; }

    public VideoTitleEntity? Title { get; set; }

    public VideoGenreEntity? Genre { get; set; }
}

public sealed class VideoAlternateTitleEntity
{
    public Guid Id { get; set; }

    public Guid TitleId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Language { get; set; }

    public VideoAlternateTitleKind Kind { get; set; }

    public int SortOrder { get; set; }

    public VideoTitleEntity? Owner { get; set; }
}