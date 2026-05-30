using Lyo.EntityReference.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.EntityReference.Postgres.Database;

/// <summary>Fluent mapping extensions for <see cref="EntityRelationOptionalActorBase" /> columns.</summary>
public static class EntityRelationOptionalActorExtensions
{
    /// <summary>Maps subject/actor string columns; default max length 500 matches change-tracker conventions.</summary>
    public static void MapOptionalActorColumns<TEntity>(this EntityTypeBuilder<TEntity> builder, int maxLength = 500)
        where TEntity : EntityRelationOptionalActorBase
    {
        builder.ConfigureEntityRelationEndpointColumns(maxLength, maxLength);
        builder.Property(e => e.TenantId).HasColumnName("tenant_id").HasColumnType("uuid");
    }
}
