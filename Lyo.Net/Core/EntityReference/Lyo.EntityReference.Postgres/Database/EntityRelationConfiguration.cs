using Lyo.EntityReference.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.EntityReference.Postgres.Database;

/// <summary>Maps canonical columns and indexes for <see cref="EntityRelationEntityBase" /> subclasses.</summary>
/// <typeparam name="TEntity">Concrete relation entity type.</typeparam>
public abstract class EntityRelationConfiguration<TEntity> : IEntityTypeConfiguration<TEntity>
    where TEntity : EntityRelationEntityBase
{
    private readonly string _indexPrefix;

    /// <summary>Creates configuration using <paramref name="indexPrefix" /> for PostgreSQL index names (for example <c>favorite</c>, <c>tag</c>).</summary>
    protected EntityRelationConfiguration(string indexPrefix)
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

    /// <summary>Maps shared relation columns; invoke from <see cref="Configure" /> after <c>ToTable</c> / <c>HasKey</c>.</summary>
    protected void MapColumns(EntityTypeBuilder<TEntity> builder)
    {
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.ConfigureEntityRelationEndpointColumns();
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
    protected void MapIndexes(EntityTypeBuilder<TEntity> builder)
    {
        builder.HasIndex(e => new {
                e.TenantId,
                e.SubjectEntityType,
                e.SubjectEntityId,
                e.ActorEntityType,
                e.ActorEntityId
            })
            .IsUnique()
            .HasDatabaseName($"uq_{_indexPrefix}_tenant_for_from_active")
            .HasFilter("\"deleted_at\" IS NULL");

        builder.HasIndex(e => new { e.TenantId, e.SubjectEntityType, e.SubjectEntityId }).HasDatabaseName($"ix_{_indexPrefix}_tenant_for_entity");
        builder.HasIndex(e => new { e.TenantId, e.ActorEntityType, e.ActorEntityId }).HasDatabaseName($"ix_{_indexPrefix}_tenant_from_entity");
        builder.HasIndex(e => new { e.TenantId, e.Context }).HasDatabaseName($"ix_{_indexPrefix}_tenant_context");
        builder.HasIndex(e => e.CreatedAt).HasDatabaseName($"ix_{_indexPrefix}_created_at");
        builder.HasIndex(e => e.ExpiresAt).HasDatabaseName($"ix_{_indexPrefix}_expires_at").HasFilter("\"expires_at\" IS NOT NULL");
    }
}