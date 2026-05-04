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
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired().HasColumnName("name");
        builder.Property(e => e.TagType).HasMaxLength(50).IsRequired().HasColumnName("tag_type").HasDefaultValue("tag");
        builder.Property(e => e.Slug).HasMaxLength(200).IsRequired().HasColumnName("slug").HasDefaultValue("");
        builder.Property(e => e.FromEntityType).HasMaxLength(200).HasColumnName("from_entity_type");
        builder.Property(e => e.FromEntityId).HasMaxLength(200).HasColumnName("from_entity_id");
        builder.Property(e => e.CreatedTimestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
        builder.HasIndex(e => new { e.ForEntityType, e.ForEntityId }).HasDatabaseName("ix_tag_for_entity");
        builder.HasIndex(e => e.Name).HasDatabaseName("ix_tag_name");
        builder.HasIndex(e => e.TagType).HasDatabaseName("ix_tag_tag_type");
        builder.HasIndex(e => new {
                e.ForEntityType,
                e.ForEntityId,
                e.TagType,
                e.Name,
                e.Slug
            })
            .IsUnique()
            .HasDatabaseName("ix_tag_for_entity_name_unique");
    }
}