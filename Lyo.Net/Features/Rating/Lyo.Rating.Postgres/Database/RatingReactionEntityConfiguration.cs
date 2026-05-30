using Lyo.EntityReference.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Rating.Postgres.Database;

public sealed class RatingReactionEntityConfiguration : IEntityTypeConfiguration<RatingReactionEntity>
{
    public void Configure(EntityTypeBuilder<RatingReactionEntity> builder)
    {
        builder.ToTable("rating_reaction");
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
            .HasDatabaseName("ix_rating_reaction_for_from")
            .IsUnique();

        builder.HasIndex(e => new { e.SubjectEntityType, e.SubjectEntityId }).HasDatabaseName("ix_rating_reaction_for_entity");
        builder.HasIndex(e => e.TenantId).HasDatabaseName("ix_rating_reaction_tenant").HasFilter("\"tenant_id\" IS NOT NULL");
    }
}
