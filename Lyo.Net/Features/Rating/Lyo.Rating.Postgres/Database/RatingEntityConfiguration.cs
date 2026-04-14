using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Rating.Postgres.Database;

public sealed class RatingEntityConfiguration : IEntityTypeConfiguration<RatingEntity>
{
    public void Configure(EntityTypeBuilder<RatingEntity> builder)
    {
        builder.ToTable("rating");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.ForEntityType).HasMaxLength(200).IsRequired().HasColumnName("for_entity_type");
        builder.Property(e => e.ForEntityId).HasMaxLength(200).IsRequired().HasColumnName("for_entity_id");
        builder.Property(e => e.FromEntityType).HasMaxLength(200).IsRequired().HasColumnName("from_entity_type");
        builder.Property(e => e.FromEntityId).HasMaxLength(200).IsRequired().HasColumnName("from_entity_id");
        builder.Property(e => e.Subject).HasMaxLength(200).HasColumnName("subject");
        builder.Property(e => e.Title).HasMaxLength(500).HasColumnName("title");
        builder.Property(e => e.Value).HasColumnName("value");
        builder.Property(e => e.Message).HasMaxLength(4000).HasColumnName("message");
        builder.Property(e => e.LikeCount).HasColumnName("like_count");
        builder.Property(e => e.DislikeCount).HasColumnName("dislike_count");
        builder.Property(e => e.CreatedTimestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
        builder.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
        builder.HasIndex(e => new { e.ForEntityType, e.ForEntityId }).HasDatabaseName("ix_rating_for_entity");
        builder.HasIndex(e => new { e.FromEntityType, e.FromEntityId }).HasDatabaseName("ix_rating_from_entity");
        builder.HasIndex(e => new {
                e.ForEntityType,
                e.ForEntityId,
                e.FromEntityType,
                e.FromEntityId,
                e.Subject
            })
            .IsUnique()
            .HasDatabaseName("ix_rating_for_from_subject_unique");
    }
}