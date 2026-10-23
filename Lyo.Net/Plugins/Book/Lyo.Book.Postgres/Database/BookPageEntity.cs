using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using NpgsqlTypes;

namespace Lyo.Book.Postgres.Database;

[DebuggerDisplay("{ToString(),nq}")]
public sealed class BookPageEntity
{
    [Key]
    public Guid Id { get; set; }

    public Guid ChapterId { get; set; }

    public int PageNumber { get; set; }

    public string? PrintPageLabel { get; set; }

    public string? Text { get; set; }

    public NpgsqlTsVector SearchVector { get; set; } = null!;

    public string ContentType { get; set; } = BookContentTypes.PlainText;

    public int CharacterCount { get; set; }

    public int WordCount { get; set; }

    public string? ScanFileRef { get; set; }

    public int? Width { get; set; }

    public int? Height { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    public BookChapterEntity Chapter { get; set; } = null!;

    /// <summary>Type name and id.</summary>
    public override string ToString() => $"{GetType().Name} {Id}";
}