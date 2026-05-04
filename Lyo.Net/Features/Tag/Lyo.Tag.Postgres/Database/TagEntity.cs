using System.ComponentModel.DataAnnotations;

namespace Lyo.Tag.Postgres.Database;

/// <summary>Entity for storing tags in PostgreSQL.</summary>
public sealed class TagEntity
{
    /// <summary>Gets or sets the unique identifier (UUID).</summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>Gets or sets the entity type being tagged (e.g. "Docket", "Person").</summary>
    [Required]
    [MaxLength(200)]
    public string ForEntityType { get; set; } = string.Empty;

    /// <summary>Gets or sets the entity id being tagged.</summary>
    [Required]
    [MaxLength(200)]
    public string ForEntityId { get; set; } = string.Empty;

    /// <summary>Gets or sets the tag display value (e.g. "urgent", "follow-up").</summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the tag type (e.g. "tag", "category"). Defaults to "tag".</summary>
    [Required]
    [MaxLength(50)]
    public string TagType { get; set; } = "tag";

    /// <summary>Gets or sets an optional URL-friendly slug for this tag assignment.</summary>
    [Required]
    [MaxLength(200)]
    public string Slug { get; set; } = string.Empty;

    /// <summary>Gets or sets the entity type of who applied the tag, or null if system-applied.</summary>
    [MaxLength(200)]
    public string? FromEntityType { get; set; }

    /// <summary>Gets or sets the entity id of who applied the tag, or null if system-applied.</summary>
    [MaxLength(200)]
    public string? FromEntityId { get; set; }

    /// <summary>Gets or sets when the tag was applied.</summary>
    [Required]
    public DateTime CreatedTimestamp { get; set; }
}