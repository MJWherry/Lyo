using Lyo.EntityReference.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.EntityReference.Postgres;

/// <summary>Shared EF configuration for <see cref="EntitySourceEntityBase" /> columns.</summary>
public static class EntitySourceLinkConfigurationExtensions
{
    public const int SourceEntityTypeMaxLength = 128;
    public const int SourceEntityIdMaxLength = 256;

    /// <summary>Maps inline provenance columns and lookup indexes on parent tables.</summary>
    public static void ConfigureEntitySourceColumns<TEntity>(this EntityTypeBuilder<TEntity> builder, string indexPrefix)
        where TEntity : EntitySourceEntityBase
    {
        builder.Property(e => e.SourceEntityType).HasMaxLength(SourceEntityTypeMaxLength).HasColumnName("source_entity_type");
        builder.Property(e => e.SourceEntityId).HasMaxLength(SourceEntityIdMaxLength).HasColumnName("source_entity_id");
        builder.Property(e => e.ImportedAt).HasColumnType("timestamp with time zone").HasColumnName("imported_at");

        builder.HasIndex(e => new { e.SourceEntityType, e.SourceEntityId })
            .HasDatabaseName($"ix_{indexPrefix}_source_lookup");

        builder.HasIndex(e => new { e.SourceEntityType, e.SourceEntityId })
            .IsUnique()
            .HasDatabaseName($"uq_{indexPrefix}_source")
            .HasFilter("\"source_entity_type\" IS NOT NULL AND \"source_entity_id\" IS NOT NULL");
    }

    /// <summary>Maps <see cref="EntitySourceDerivedEntityBase" /> lifecycle columns on parent aggregates.</summary>
    public static void ConfigureEntitySourceDerivedColumns<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : EntitySourceDerivedEntityBase
    {
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.LocallyModifiedAt).HasColumnName("locally_modified_at");
    }
}
