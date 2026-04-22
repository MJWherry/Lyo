using System.ComponentModel.DataAnnotations;

namespace Lyo.Favorite.Postgres.Database;

/// <summary>Entity for storing favorites in PostgreSQL.</summary>
public sealed class FavoriteEntity
{
    /// <summary>Gets or sets the unique identifier (UUID).</summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>Gets or sets the entity type being favorited (e.g. "Article", "Product").</summary>
    [Required]
    [MaxLength(200)]
    public string ForEntityType { get; set; } = string.Empty;

    /// <summary>Gets or sets the entity id being favorited.</summary>
    [Required]
    [MaxLength(200)]
    public string ForEntityId { get; set; } = string.Empty;

    /// <summary>Gets or sets the entity type of who added the favorite (e.g. "User").</summary>
    [Required]
    [MaxLength(200)]
    public string FromEntityType { get; set; } = string.Empty;

    /// <summary>Gets or sets the entity id of who added the favorite.</summary>
    [Required]
    [MaxLength(200)]
    public string FromEntityId { get; set; } = string.Empty;

    /// <summary>Gets or sets when the favorite was created.</summary>
    [Required]
    public DateTime CreatedTimestamp { get; set; }
}