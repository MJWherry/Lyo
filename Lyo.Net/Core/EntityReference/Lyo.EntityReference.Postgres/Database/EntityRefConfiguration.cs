using Lyo.EntityReference.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.EntityReference.Postgres.Database;

/// <summary>Maps canonical columns and indexes for <see cref="EntityRefEntityBase"/> subclasses.</summary>
/// <typeparam name="TEntity">Concrete association entity type.</typeparam>
public abstract class EntityRefConfiguration<TEntity> : IEntityTypeConfiguration<TEntity>
    where TEntity : EntityRefEntityBase
{
    readonly string _indexPrefix;

    /// <summary>Creates configuration using <paramref name="indexPrefix"/> for PostgreSQL index names (for example <c>favorite</c>, <c>tag</c>).</summary>
    /// <param name="indexPrefix">Short snake-case prefix embedded in generated index names.</param>
    /// <exception cref="ArgumentException"><paramref name="indexPrefix"/> is null or whitespace.</exception>
    protected EntityRefConfiguration(string indexPrefix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(indexPrefix);
        _indexPrefix = indexPrefix;
    }

    /// <inheritdoc />
    public virtual void Configure(EntityTypeBuilder<TEntity> builder)
    {
        MapColumns(builder);
        MapIndexes(builder);
    }

    /// <summary>Maps shared association columns; invoke from <see cref="Configure"/> after <c>ToTable</c> / <c>HasKey</c>.</summary>
    /// <param name="builder">Fluent builder for <typeparamref name="TEntity"/>.</param>
    protected void MapColumns(EntityTypeBuilder<TEntity> builder)
    {
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.ForEntityType).HasMaxLength(200).IsRequired().HasColumnName("for_entity_type");
        builder.Property(e => e.ForEntityId).IsRequired().HasColumnName("for_entity_id").HasColumnType("uuid");
        builder.Property(e => e.FromEntityType).HasMaxLength(200).IsRequired().HasColumnName("from_entity_type");
        builder.Property(e => e.FromEntityId).IsRequired().HasColumnName("from_entity_id").HasColumnType("uuid");
        builder.Property(e => e.TenantId).IsRequired().HasColumnName("tenant_id").HasColumnType("uuid");
        builder.Property(e => e.Context).HasMaxLength(200).HasColumnName("context");
        builder.Property(e => e.CreatedAt).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_at");
        builder.Property(e => e.ExpiresAt).HasColumnType("timestamp with time zone").HasColumnName("expires_at");
        builder.Property(e => e.DeletedAt).HasColumnType("timestamp with time zone").HasColumnName("deleted_at");
        builder.Property(e => e.DeletedByType).HasMaxLength(200).HasColumnName("deleted_by_type");
        builder.Property(e => e.DeletedById).HasColumnName("deleted_by_id").HasColumnType("uuid");
        builder.Property(e => e.MetadataJson).HasColumnName("metadata").HasColumnType("jsonb");
        builder.Property(e => e.Visibility).HasMaxLength(64).IsRequired().HasColumnName("visibility").HasDefaultValue(EntityRefVisibility.Private);
    }

    /// <summary>Maps shared btree indexes (tenant scoped, soft-delete partial unique, expiry filter).</summary>
    /// <param name="builder">Fluent builder for <typeparamref name="TEntity"/>.</param>
    protected void MapIndexes(EntityTypeBuilder<TEntity> builder)
    {
        builder.HasIndex(e => new { e.TenantId, e.ForEntityType, e.ForEntityId, e.FromEntityType, e.FromEntityId })
            .IsUnique()
            .HasDatabaseName($"uq_{_indexPrefix}_tenant_for_from_active")
            .HasFilter("\"deleted_at\" IS NULL");

        builder.HasIndex(e => new { e.TenantId, e.ForEntityType, e.ForEntityId }).HasDatabaseName($"ix_{_indexPrefix}_tenant_for_entity");

        builder.HasIndex(e => new { e.TenantId, e.FromEntityType, e.FromEntityId }).HasDatabaseName($"ix_{_indexPrefix}_tenant_from_entity");

        builder.HasIndex(e => new { e.TenantId, e.Context }).HasDatabaseName($"ix_{_indexPrefix}_tenant_context");

        builder.HasIndex(e => e.CreatedAt).HasDatabaseName($"ix_{_indexPrefix}_created_at");

        builder.HasIndex(e => e.ExpiresAt).HasDatabaseName($"ix_{_indexPrefix}_expires_at").HasFilter("\"expires_at\" IS NOT NULL");
    }
}
