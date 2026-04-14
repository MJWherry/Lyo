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
        builder.Property(e => e.ForEntityType).HasMaxLength(200).IsRequired().HasColumnName("for_entity_type");
        builder.Property(e => e.ForEntityId).HasMaxLength(200).IsRequired().HasColumnName("for_entity_id");
        builder.Property(e => e.FromEntityType).HasMaxLength(200).IsRequired().HasColumnName("from_entity_type");
        builder.Property(e => e.FromEntityId).HasMaxLength(200).IsRequired().HasColumnName("from_entity_id");
        builder.Property(e => e.ReactionType).HasColumnName("reaction_type");
        builder.Property(e => e.CreatedTimestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
        builder.HasIndex(e => new {
                e.ForEntityType,
                e.ForEntityId,
                e.FromEntityType,
                e.FromEntityId
            })
            .HasDatabaseName("ix_rating_reaction_for_from")
            .IsUnique();

        builder.HasIndex(e => new { e.ForEntityType, e.ForEntityId }).HasDatabaseName("ix_rating_reaction_for_entity");
    }
}