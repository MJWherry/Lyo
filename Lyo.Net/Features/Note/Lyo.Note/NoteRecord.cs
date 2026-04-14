using Lyo.Common;

namespace Lyo.Note;

/// <summary>Represents a note attached to an entity.</summary>
public sealed class NoteRecord
{
    /// <summary>Gets or sets the unique identifier of the note.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the entity type the note is for (e.g. "Docket", "Person").</summary>
    public string ForEntityType { get; set; } = string.Empty;

    /// <summary>Gets or sets the entity id the note is for (typically a Guid string, or any string key).</summary>
    public string ForEntityId { get; set; } = string.Empty;

    /// <summary>Gets or sets the entity type of the author (e.g. "User").</summary>
    public string FromEntityType { get; set; } = string.Empty;

    /// <summary>Gets or sets the entity id of the author (e.g. user id 123).</summary>
    public string FromEntityId { get; set; } = string.Empty;

    /// <summary>Gets or sets the note content.</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>Gets or sets when the note was created.</summary>
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>Gets or sets when the note was last updated.</summary>
    public DateTime? UpdatedTimestamp { get; set; }

    /// <summary>Gets the entity reference for what the note is about.</summary>
    public EntityRef ForEntity => new(ForEntityType, ForEntityId);

    /// <summary>Gets the entity reference for who created the note.</summary>
    public EntityRef FromEntity => new(FromEntityType, FromEntityId);
}