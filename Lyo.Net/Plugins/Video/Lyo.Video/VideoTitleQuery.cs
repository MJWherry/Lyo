using Lyo.Video.Enums;

namespace Lyo.Video;

/// <summary>Filter for title search. <see cref="FilterIds" /> null means no id filter. An empty list returns no rows.</summary>
public sealed class VideoTitleQuery
{
    public string? TitleContains { get; set; }

    public VideoTitleKind? Kind { get; set; }

    public Guid? GenreId { get; set; }

    public int? Year { get; set; }

    public string? IdentifierScheme { get; set; }

    public string? IdentifierValue { get; set; }

    public IReadOnlyList<Guid>? FilterIds { get; set; }
}