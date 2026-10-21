namespace Lyo.Audiobook;

/// <summary>Filter for work search. <see cref="FilterIds" /> null means no id filter. An empty list returns no rows.</summary>
public sealed class AudiobookQuery
{
    public string? TitleContains { get; set; }

    public Guid? GenreId { get; set; }

    public int? Year { get; set; }

    public string? IdentifierScheme { get; set; }

    public string? IdentifierValue { get; set; }

    public IReadOnlyList<Guid>? FilterIds { get; set; }
}