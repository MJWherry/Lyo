using Lyo.EntityReference.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Comment.Postgres.Database;

public sealed class CommentReactionEntityConfiguration : IEntityTypeConfiguration<CommentReactionEntity>
{
    public void Configure(EntityTypeBuilder<CommentReactionEntity> builder)
    {
        builder.ToTable("comment_reaction");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.ConfigureEntityRelationEndpointColumns();
        builder.Property(e => e.ReactionType).HasColumnName("reaction_type");
        builder.Property(e => e.TenantId).HasColumnType("uuid").HasColumnName("tenant_id");
        builder.Property(e => e.CreatedTimestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
        builder.HasIndex(e => new {
                e.SubjectEntityType,
                e.SubjectEntityId,
                e.ActorEntityType,
                e.ActorEntityId
            })
            .HasDatabaseName("ix_comment_reaction_for_from")
            .IsUnique();

        builder.HasIndex(e => new { e.SubjectEntityType, e.SubjectEntityId }).HasDatabaseName("ix_comment_reaction_for_entity");
        builder.HasIndex(e => e.TenantId).HasDatabaseName("ix_comment_reaction_tenant").HasFilter("\"tenant_id\" IS NOT NULL");
    }
}
