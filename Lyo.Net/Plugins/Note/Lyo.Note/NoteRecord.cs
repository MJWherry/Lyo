using Lyo.EntityReference.Models;

namespace Lyo.Note;

/// <summary>Model for a note attached to an entity (canonical entity-ref row + content).</summary>
public sealed class NoteRecord : EntityRelationRow
{
    /// <summary>Note content.</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>UTC time of the last write.</summary>
    public DateTime? UpdatedTimestamp { get; set; }
}