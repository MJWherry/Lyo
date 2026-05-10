using Lyo.EntityReference.Models;

namespace Lyo.Note;

/// <summary>Represents a note attached to an entity (canonical entity-ref row + content).</summary>
public sealed class NoteRecord : EntityRefRow
{
    /// <summary>Note body.</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>Last update time (UTC).</summary>
    public DateTime? UpdatedTimestamp { get; set; }

    /// <summary>Gets the entity reference for what the note is about.</summary>
    public EntityRef ForEntity => EntityRef.ForGuid(ForEntityType, ForEntityId);

    /// <summary>Gets the entity reference for who created the note.</summary>
    public EntityRef FromEntity => EntityRef.ForGuid(FromEntityType, FromEntityId);
}
