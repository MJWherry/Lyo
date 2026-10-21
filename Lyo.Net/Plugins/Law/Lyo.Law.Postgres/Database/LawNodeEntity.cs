using Lyo.Law.Models.Enums;
using NpgsqlTypes;

namespace Lyo.Law.Postgres.Database;

/// <summary>One outline or text unit in a state code.</summary>
public sealed class LawNodeEntity
{
    /// <summary>Row id.</summary>
    public Guid Id { get; set; }

    /// <summary>Owning code.</summary>
    public Guid CodeId { get; set; }

    /// <summary>Parent node. Null for a root under the code.</summary>
    public Guid? ParentId { get; set; }

    /// <summary>Outline level.</summary>
    public LawNodeKind Kind { get; set; }

    /// <summary>Order among siblings.</summary>
    public int SortOrder { get; set; }

    /// <summary>Local designator.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Catchline or heading.</summary>
    public string Heading { get; set; } = string.Empty;

    /// <summary>Operative text.</summary>
    public string? Body { get; set; }

    /// <summary>Stored full-text vector of <see cref="Body" />.</summary>
    public NpgsqlTsVector SearchVector { get; set; } = null!;

    /// <summary>Display citation. Unique per code when set.</summary>
    public string? Citation { get; set; }

    /// <summary>Whether the provision is repealed.</summary>
    public bool IsRepealed { get; set; }

    /// <summary>SHA-256 hex of the stored content.</summary>
    public string ContentSha256 { get; set; } = string.Empty;

    /// <summary>Opaque importer key, unique per code.</summary>
    public string SourceKey { get; set; } = string.Empty;

    /// <summary>Page the text came from.</summary>
    public string? SourceUrl { get; set; }

    /// <summary>When the row was inserted.</summary>
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>When the row was last updated.</summary>
    public DateTime? UpdatedTimestamp { get; set; }

    /// <summary>Owning code.</summary>
    public LawCodeEntity Code { get; set; } = null!;

    /// <summary>Parent node.</summary>
    public LawNodeEntity? Parent { get; set; }

    /// <summary>Child nodes.</summary>
    public List<LawNodeEntity> Children { get; set; } = [];

    /// <summary>Official notes.</summary>
    public List<LawAnnotationEntity> Annotations { get; set; } = [];
}