using Lyo.EntityReference.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.EntityReference.Postgres;

/// <summary>Shared EF configuration for <see cref="EntityRelationEndpointsEntityBase" /> columns.</summary>
public static class EntityRelationEndpointConfigurationExtensions
{
    public const int EntityTypeMaxLength = 128;
    public const int EntityIdMaxLength = 256;

    /// <summary>Maps nullable subject/actor endpoint columns (persisted as for_entity_* / from_entity_*).</summary>
    public static void ConfigureEntityRelationEndpointColumns<TEntity>(this EntityTypeBuilder<TEntity> builder, int entityTypeMaxLength = EntityTypeMaxLength, int entityIdMaxLength = EntityIdMaxLength)
        where TEntity : EntityRelationEndpointsEntityBase
    {
        builder.Property(e => e.SubjectEntityType).HasMaxLength(entityTypeMaxLength).HasColumnName("for_entity_type");
        builder.Property(e => e.SubjectEntityId).HasMaxLength(entityIdMaxLength).HasColumnName("for_entity_id");
        builder.Property(e => e.ActorEntityType).HasMaxLength(entityTypeMaxLength).HasColumnName("from_entity_type");
        builder.Property(e => e.ActorEntityId).HasMaxLength(entityIdMaxLength).HasColumnName("from_entity_id");
    }
}
