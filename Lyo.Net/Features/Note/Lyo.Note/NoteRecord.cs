using Lyo.EntityReference.Models;

namespace Lyo.Note;

/// <summary>Represents a note attached to an entity (canonical entity-ref row + content).</summary>
public sealed class NoteRecord : EntityRelationRow
{
    /// <summary>Note body.</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>Last update time (UTC).</summary>
    public DateTime? UpdatedTimestamp { get; set; }
}
