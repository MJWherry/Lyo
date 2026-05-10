using Lyo.EntityReference.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Comment.Postgres.Database;

public sealed class CommentEntityConfiguration : EntityRefConfiguration<CommentEntity>
{
    public CommentEntityConfiguration()
        : base("comment") { }

    /// <inheritdoc />
    public override void Configure(EntityTypeBuilder<CommentEntity> builder)
    {
        builder.ToTable("comment");
        builder.HasKey(e => e.Id);
        MapColumns(builder);
        builder.Property(e => e.Content).IsRequired().HasMaxLength(8192).HasColumnName("content");
        builder.Property(e => e.ReplyToCommentId).HasColumnName("reply_to_comment_id").HasColumnType("uuid");
        builder.Property(e => e.LikeCount).HasColumnName("like_count");
        builder.Property(e => e.DislikeCount).HasColumnName("dislike_count");
        builder.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
        builder.Property(e => e.IsEdited).HasColumnName("is_edited");
        builder.HasIndex(e => new { e.TenantId, e.ForEntityType, e.ForEntityId }).HasDatabaseName("ix_comment_tenant_for_entity");
        builder.HasIndex(e => new { e.TenantId, e.FromEntityType, e.FromEntityId }).HasDatabaseName("ix_comment_tenant_from_entity");
        builder.HasIndex(e => e.ReplyToCommentId).HasDatabaseName("ix_comment_reply_to");
        builder.HasIndex(e => new { e.TenantId, e.Context }).HasDatabaseName("ix_comment_tenant_context");
        builder.HasIndex(e => e.CreatedAt).HasDatabaseName("ix_comment_created_at");
        builder.HasIndex(e => e.ExpiresAt).HasDatabaseName("ix_comment_expires_at").HasFilter("\"expires_at\" IS NOT NULL");
    }
}
