using System.Diagnostics;
using Lyo.Book.Enums;

namespace Lyo.Book;

/// <summary>A catalog edition (one ISBN / manifestation). Optional series membership.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class Book
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Subtitle { get; set; }

    public string Slug { get; set; } = string.Empty;

    public Guid? SeriesId { get; set; }

    public decimal? SeriesIndex { get; set; }

    public string? PrimaryAuthor { get; set; }

    public Guid? TypeId { get; set; }

    public Guid? FormatId { get; set; }

    public BookStatus Status { get; set; }

    public string? Description { get; set; }

    public string? Language { get; set; }

    public string? OriginalLanguage { get; set; }

    public int? PublishedYear { get; set; }

    public DateTime? PublishedDate { get; set; }

    public string? Publisher { get; set; }

    public string? Imprint { get; set; }

    public string? EditionStatement { get; set; }

    public int? EditionNumber { get; set; }

    public int? PrintPageCount { get; set; }

    public int? WordCount { get; set; }

    public string? Audience { get; set; }

    public string? ContentRating { get; set; }

    public string? CountryOfOrigin { get; set; }

    public string? CoverImageRef { get; set; }

    public string? Source { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    public IReadOnlyList<BookAlternateTitle> AlternateTitles { get; set; } = [];

    public IReadOnlyList<BookIdentifier> Identifiers { get; set; } = [];

    public IReadOnlyList<BookContributor> Contributors { get; set; } = [];

    public IReadOnlyList<Guid> GenreIds { get; set; } = [];

    public IReadOnlyList<BookGenre> Genres { get; set; } = [];

    /// <inheritdoc />
    public override string ToString() => $"Book(Id={Id}, Title=\"{Title}\", Slug=\"{Slug}\", Status={Status})";
}
