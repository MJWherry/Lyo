namespace Lyo.Video.Postgres.Database;

public sealed class VideoCollectionEntity
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public ICollection<VideoTitleEntity> Titles { get; set; } = [];
}

public sealed class VideoCutEntity
{
    public Guid Id { get; set; }

    public Guid TitleId { get; set; }

    public string Name { get; set; } = string.Empty;

    public int? RuntimeSeconds { get; set; }

    public string? VideoFileRef { get; set; }

    public bool IsPrimary { get; set; }

    public VideoTitleEntity? Title { get; set; }
}
