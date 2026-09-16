namespace Lyo.Law.Models.Response;

/// <summary>Prior heading and body captured when a node upsert changed the content hash.</summary>
public sealed class LawRevisionRes
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
}
