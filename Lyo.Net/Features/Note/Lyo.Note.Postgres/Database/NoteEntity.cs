using System.ComponentModel.DataAnnotations;

namespace Lyo.Note.Postgres.Database;

/// <summary>Entity for storing notes in PostgreSQL.</summary>
public sealed class NoteEntity
{
    /// <summary>Gets or sets the unique identifier (UUID).</summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>Gets or sets the entity type the note is for (e.g. "Docket", "Person").</summary>
    [Required]
    [MaxLength(200)]
    public string ForEntityType { get; set; } = string.Empty;

    /// <summary>Gets or sets the entity id the note is for.</summary>
    [Required]
    [MaxLength(200)]
    public string ForEntityId { get; set; } = string.Empty;

    /// <summary>Gets or sets the entity type of the author (e.g. "User").</summary>
    [Required]
    [MaxLength(200)]
    public string FromEntityType { get; set; } = string.Empty;

    /// <summary>Gets or sets the entity id of the author.</summary>
    [Required]
    [MaxLength(200)]
    public string FromEntityId { get; set; } = string.Empty;

    /// <summary>Gets or sets the note content.</summary>
    [Required]
    public string Content { get; set; } = string.Empty;

    /// <summary>Gets or sets when the note was created.</summary>
    [Required]
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>Gets or sets when the note was last updated.</summary>
    public DateTime? UpdatedTimestamp { get; set; }
}