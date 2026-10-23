using System.Diagnostics;

namespace Lyo.FileMetadataStore.Models;

/// <summary>Prior-revision row: id, parent file id, stored key, and when it was recorded.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class FileRevisionEntity
{
    /// <summary>Revision id.</summary>
    public Guid Id { get; set; }

    /// <summary>File this revision belongs to.</summary>
    public Guid ParentId { get; set; }

    /// <summary>Stored object key before the newer bytes replaced it.</summary>
    public string StoredKey { get; set; } = "";

    /// <summary>When the revision was recorded.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Type name and id.</summary>
    public override string ToString() => $"{GetType().Name} {Id}";
}