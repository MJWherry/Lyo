using Lyo.EntityReference.Postgres.Database;

namespace Lyo.Note.Postgres.Database;

/// <summary>Entity for storing notes in PostgreSQL.</summary>
public sealed class NoteEntity : EntityRefEntityBase
{
    /// <summary>Note body.</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>Last update time (UTC).</summary>
    public DateTime? UpdatedTimestamp { get; set; }
}
