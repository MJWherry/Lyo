using System.ComponentModel.DataAnnotations;

namespace Lyo.Rating.Postgres.Database;

/// <summary>Entity for storing rating reactions (like/dislike) in PostgreSQL. Option A uuid keys for target rating and reactor.</summary>
public sealed class RatingReactionEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string ForEntityType { get; set; } = string.Empty;

    public Guid ForEntityId { get; set; }

    [Required]
    [MaxLength(200)]
    public string FromEntityType { get; set; } = string.Empty;

    public Guid FromEntityId { get; set; }

    public int ReactionType { get; set; }

    /// <summary>Optional tenant scope. <see langword="null" /> means system / no tenant; non-null indicates a tenant-scoped reaction (inherits from the parent rating at write time).</summary>
    public Guid? TenantId { get; set; }

    [Required]
    public DateTime CreatedTimestamp { get; set; }
}