namespace Lyo.Law.Postgres.Database;

/// <summary>Insert-only snapshot of a node taken when an upsert changes the content hash.</summary>
public sealed class LawRevisionEntity
{
    /// <summary>Row id.</summary>
    public Guid Id { get; set; }

    /// <summary>Node this snapshot belongs to.</summary>
    public Guid NodeId { get; set; }

    /// <summary>1-based revision number, unique per node.</summary>
    public int Revision { get; set; }

    /// <summary>Heading before the change.</summary>
    public string Heading { get; set; } = string.Empty;

    /// <summary>Body before the change.</summary>
    public string? Body { get; set; }

    /// <summary>Content hash before the change.</summary>
    public string ContentSha256 { get; set; } = string.Empty;

    /// <summary>JSON snapshot of the notes before the change.</summary>
    public string AnnotationsJson { get; set; } = "[]";

    /// <summary>When the snapshot was written.</summary>
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>Owning node.</summary>
    public LawNodeEntity Node { get; set; } = null!;
}
