using System.Diagnostics;

namespace Lyo.Book;

/// <summary>A page within a chapter. <see cref="Text" /> is omitted unless requested.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class BookPage
{
    public Guid Id { get; set; }

    public Guid ChapterId { get; set; }

    public int PageNumber { get; set; }

    public string? PrintPageLabel { get; set; }

    /// <summary>Body text when <c>includeText</c> or <see cref="IBookStore.GetPageTextAsync" /> loaded it.</summary>
    public string? Text { get; set; }

    public string ContentType { get; set; } = BookContentTypes.PlainText;

    public int CharacterCount { get; set; }

    public int WordCount { get; set; }

    public bool HasContent => CharacterCount > 0;

    public string? ScanFileRef { get; set; }

    public int? Width { get; set; }

    public int? Height { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"BookPage(Id={Id}, ChapterId={ChapterId}, PageNumber={PageNumber})";
}
