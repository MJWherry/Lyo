using Lyo.EntityReference.Postgres.Database;

namespace Lyo.Rating.Postgres.Database;

/// <summary>Entity for storing rating reactions (like/dislike) in PostgreSQL.</summary>
public sealed class RatingReactionEntity : EntityRelationEndpointsEntityBase
{
    public Guid Id { get; set; }

    public int ReactionType { get; set; }

    /// <summary>Optional tenant scope. <see langword="null" /> means system / no tenant; non-null indicates a tenant-scoped reaction (inherits from the parent rating at write time).</summary>
    public Guid? TenantId { get; set; }

    public DateTime CreatedTimestamp { get; set; }
}
