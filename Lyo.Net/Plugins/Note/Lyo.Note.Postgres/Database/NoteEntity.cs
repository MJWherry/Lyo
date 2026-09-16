using Lyo.EntityReference.Postgres.Database;
using NpgsqlTypes;

namespace Lyo.Note.Postgres.Database;

/// <summary>Postgres row that holds notes in PostgreSQL.</summary>
public sealed class NoteEntity : EntityRelationEntityBase
{
    /// <summary>Note content.</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>Stored full-text vector of <see cref="Content" />.</summary>
    public NpgsqlTsVector SearchVector { get; set; } = null!;

    /// <summary>UTC time of the last write.</summary>
    public DateTime? UpdatedTimestamp { get; set; }
}