using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.EntityReference.Postgres.Database;

/// <summary>Fluent mapping helpers for <see cref="EntityRelationOptionalActorBase" /> columns.</summary>
public static class EntityRelationOptionalActorExtensions
{
    /// <summary>Maps subject/actor string columns; the default max length of 500 matches change-tracker conventions.</summary>
    public static void MapOptionalActorColumns<TEntity>(this EntityTypeBuilder<TEntity> builder, int maxLength = 500)
        where TEntity : EntityRelationOptionalActorBase
    {
        builder.ConfigureEntityRelationEndpointColumns(maxLength, maxLength);
        builder.Property(e => e.TenantId).HasColumnName("tenant_id").HasColumnType("uuid");
    }
}