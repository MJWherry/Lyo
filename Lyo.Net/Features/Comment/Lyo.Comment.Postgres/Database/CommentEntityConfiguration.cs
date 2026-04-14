using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Comment.Postgres.Database;

public sealed class CommentEntityConfiguration : IEntityTypeConfiguration<CommentEntity>
{
    public void Configure(EntityTypeBuilder<CommentEntity> builder)
    {
        builder.ToTable("comment");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.ForEntityType).HasMaxLength(200).IsRequired().HasColumnName("for_entity_type");
        builder.Property(e => e.ForEntityId).HasMaxLength(200).IsRequired().HasColumnName("for_entity_id");
        builder.Property(e => e.FromEntityType).HasMaxLength(200).IsRequired().HasColumnName("from_entity_type");
        builder.Property(e => e.FromEntityId).HasMaxLength(200).IsRequired().HasColumnName("from_entity_id");
        builder.Property(e => e.Content).IsRequired().HasColumnName("content");
        builder.Property(e => e.ReplyToCommentId).HasColumnName("reply_to_comment_id").HasColumnType("uuid");
        builder.Property(e => e.LikeCount).HasColumnName("like_count");
        builder.Property(e => e.DislikeCount).HasColumnName("dislike_count");
        builder.Property(e => e.CreatedTimestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
        builder.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
        builder.Property(e => e.IsEdited).HasColumnName("is_edited");
        builder.HasIndex(e => new { e.ForEntityType, e.ForEntityId }).HasDatabaseName("ix_comment_for_entity");
        builder.HasIndex(e => new { e.FromEntityType, e.FromEntityId }).HasDatabaseName("ix_comment_from_entity");
        builder.HasIndex(e => e.ReplyToCommentId).HasDatabaseName("ix_comment_reply_to");
    }
}