using Lyo.Music.Enums;

namespace Lyo.Music;

/// <summary>Filter for release search. <see cref="FilterIds" /> null means no id filter. An empty list returns no rows.</summary>
public sealed class MusicReleaseQuery
{
    public string? TitleContains { get; set; }

    public MusicReleaseKind? Kind { get; set; }

    public Guid? GenreId { get; set; }

    public Guid? FormatId { get; set; }

    public int? Year { get; set; }

    public string? IdentifierScheme { get; set; }

    public string? IdentifierValue { get; set; }

    public IReadOnlyList<Guid>? FilterIds { get; set; }
}
