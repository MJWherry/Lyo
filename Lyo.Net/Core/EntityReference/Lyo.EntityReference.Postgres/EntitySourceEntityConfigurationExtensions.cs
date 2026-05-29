using Lyo.EntityReference.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.EntityReference.Postgres;

/// <summary>Shared EF configuration for <see cref="EntitySourceEntityBase" /> columns.</summary>
public static class EntitySourceEntityConfigurationExtensions
{
    public const int SourceEntityTypeMaxLength = 128;
    public const int SourceEntityIdMaxLength = 256;

    /// <summary>Maps provenance columns and unique index on source key.</summary>
    public static void ConfigureEntitySourceColumns<TSource>(this EntityTypeBuilder<TSource> builder, string uniqueIndexName)
        where TSource : EntitySourceEntityBase
    {
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.SourceEntityType).HasMaxLength(SourceEntityTypeMaxLength).IsRequired().HasColumnName("source_entity_type");
        builder.Property(e => e.SourceEntityId).HasMaxLength(SourceEntityIdMaxLength).IsRequired().HasColumnName("source_entity_id");
        builder.Property(e => e.ImportedAt).HasColumnName("imported_at");
        builder.Property(e => e.FromEntityType).HasMaxLength(SourceEntityTypeMaxLength).HasColumnName("from_entity_type");
        builder.Property(e => e.FromEntityId).HasMaxLength(SourceEntityIdMaxLength).HasColumnName("from_entity_id");
        builder.HasIndex(e => new { e.SourceEntityType, e.SourceEntityId }).IsUnique().HasDatabaseName(uniqueIndexName);
    }
}