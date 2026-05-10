using Lyo.EntityReference.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Rating.Postgres.Database;

public sealed class RatingEntityConfiguration : EntityRefConfiguration<RatingEntity>
{
    public RatingEntityConfiguration()
        : base("rating") { }

    /// <inheritdoc />
    public override void Configure(EntityTypeBuilder<RatingEntity> builder)
    {
        builder.ToTable("rating");
        builder.HasKey(e => e.Id);
        MapColumns(builder);
        builder.Property(e => e.Subject).HasMaxLength(200).HasColumnName("subject");
        builder.Property(e => e.Title).HasMaxLength(500).HasColumnName("title");
        builder.Property(e => e.Value).HasColumnName("value");
        builder.Property(e => e.Message).HasMaxLength(4000).HasColumnName("message");
        builder.Property(e => e.LikeCount).HasColumnName("like_count");
        builder.Property(e => e.DislikeCount).HasColumnName("dislike_count");
        builder.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
        builder.HasIndex(e => new { e.TenantId, e.ForEntityType, e.ForEntityId }).HasDatabaseName("ix_rating_tenant_for_entity");
        builder.HasIndex(e => new { e.TenantId, e.FromEntityType, e.FromEntityId }).HasDatabaseName("ix_rating_tenant_from_entity");
        builder.HasIndex(e => new { e.TenantId, e.ForEntityType, e.ForEntityId, e.FromEntityType, e.FromEntityId, e.Subject }).HasDatabaseName("ix_rating_tenant_for_from_subject");
        builder.HasIndex(e => new { e.TenantId, e.Context }).HasDatabaseName("ix_rating_tenant_context");
        builder.HasIndex(e => e.CreatedAt).HasDatabaseName("ix_rating_created_at");
        builder.HasIndex(e => e.ExpiresAt).HasDatabaseName("ix_rating_expires_at").HasFilter("\"expires_at\" IS NOT NULL");
    }
}
