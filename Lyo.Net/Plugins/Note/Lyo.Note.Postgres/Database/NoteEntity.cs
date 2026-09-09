using Lyo.EntityReference.Postgres.Database;

namespace Lyo.Note.Postgres.Database;

/// <summary>Postgres row that holds notes in PostgreSQL.</summary>
public sealed class NoteEntity : EntityRelationEntityBase
{
    /// <summary>Note content.</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>UTC time of the last write.</summary>
    public DateTime? UpdatedTimestamp { get; set; }
}