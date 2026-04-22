using Lyo.Common;
using Lyo.Common.Identifiers;

namespace Lyo.Favorite;

/// <summary>Represents a favorite relationship between two entities.</summary>
public sealed class FavoriteRecord
{
    /// <summary>Gets or sets the unique identifier of the favorite.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the entity type being favorited (e.g. "Article", "Product").</summary>
    public string ForEntityType { get; set; } = string.Empty;

    /// <summary>Gets or sets the entity id being favorited.</summary>
    public string ForEntityId { get; set; } = string.Empty;

    /// <summary>Gets or sets the entity type of who added the favorite (e.g. "User").</summary>
    public string FromEntityType { get; set; } = string.Empty;

    /// <summary>Gets or sets the entity id of who added the favorite.</summary>
    public string FromEntityId { get; set; } = string.Empty;

    /// <summary>Gets or sets when the favorite was created.</summary>
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>Gets the entity reference for what is being favorited.</summary>
    public EntityRef ForEntity => new(ForEntityType, ForEntityId);

    /// <summary>Gets the entity reference for who added the favorite.</summary>
    public EntityRef FromEntity => new(FromEntityType, FromEntityId);
}
