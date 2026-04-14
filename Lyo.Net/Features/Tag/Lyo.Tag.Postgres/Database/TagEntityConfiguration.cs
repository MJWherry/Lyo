using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Tag.Postgres.Database;

public sealed class TagEntityConfiguration : IEntityTypeConfiguration<TagEntity>
{
    public void Configure(EntityTypeBuilder<TagEntity> builder)
    {
        builder.ToTable("tag");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.ForEntityType).HasMaxLength(200).IsRequired().HasColumnName("for_entity_type");
        builder.Property(e => e.ForEntityId).HasMaxLength(200).IsRequired().HasColumnName("for_entity_id");
        builder.Property(e => e.Tag).HasMaxLength(200).IsRequired().HasColumnName("tag");
        builder.Property(e => e.FromEntityType).HasMaxLength(200).HasColumnName("from_entity_type");
        builder.Property(e => e.FromEntityId).HasMaxLength(200).HasColumnName("from_entity_id");
        builder.Property(e => e.CreatedTimestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
        builder.HasIndex(e => new { e.ForEntityType, e.ForEntityId }).HasDatabaseName("ix_tag_for_entity");
        builder.HasIndex(e => e.Tag).HasDatabaseName("ix_tag_tag");
        builder.HasIndex(e => new { e.ForEntityType, e.ForEntityId, e.Tag }).IsUnique().HasDatabaseName("ix_tag_for_entity_tag_unique");
    }
}