using System.ComponentModel.DataAnnotations;
using Lyo.Book.Enums;

namespace Lyo.Book.Postgres.Database;

public sealed class BookEntity
{
    [Key]
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

    public DateOnly? PublishedDate { get; set; }

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

    public BookSeriesEntity? Series { get; set; }

    public BookTypeEntity? Type { get; set; }

    public BookFormatEntity? Format { get; set; }

    public ICollection<BookAlternateTitleEntity> AlternateTitles { get; set; } = [];

    public ICollection<BookIdentifierEntity> Identifiers { get; set; } = [];

    public ICollection<BookContributorEntity> Contributors { get; set; } = [];

    public ICollection<BookBookGenreEntity> GenreLinks { get; set; } = [];

    public ICollection<BookChapterEntity> Chapters { get; set; } = [];
}
