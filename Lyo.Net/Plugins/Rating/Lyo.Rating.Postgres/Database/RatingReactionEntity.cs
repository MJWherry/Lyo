using Lyo.EntityReference.Postgres.Database;

namespace Lyo.Rating.Postgres.Database;

/// <summary>Postgres row that holds rating reactions (like/dislike) in PostgreSQL.</summary>
public sealed class RatingReactionEntity : EntityRelationEndpointsEntityBase
{
    public Guid Id { get; set; }

    public int ReactionType { get; set; }

    /// <summary>
    /// tenant scope. <see langword="null" /> means system / no tenant; non-null indicates a tenant-scoped reaction (inherits from the parent rating at write time). Present only
    /// when supplied.
    /// </summary>
    public Guid? TenantId { get; set; }

    public DateTime CreatedTimestamp { get; set; }
}