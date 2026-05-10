using Lyo.EntityReference.Models;

namespace Lyo.Favorite;

/// <summary>Represents a favorite relationship between two entities (canonical entity-ref row + module key).</summary>
public sealed class FavoriteRecord : EntityRefRow
{
    /// <summary>Gets the entity reference for what is being favorited.</summary>
    public EntityRef ForEntity => EntityRef.ForGuid(ForEntityType, ForEntityId);

    /// <summary>Gets the entity reference for who added the favorite.</summary>
    public EntityRef FromEntity => EntityRef.ForGuid(FromEntityType, FromEntityId);
}
