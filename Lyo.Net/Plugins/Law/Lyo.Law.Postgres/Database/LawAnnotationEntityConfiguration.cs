using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Law.Postgres.Database;

/// <summary>Maps <see cref="LawAnnotationEntity"/> to <c>law.annotation</c>.</summary>
public sealed class LawAnnotationEntityConfiguration : IEntityTypeConfiguration<LawAnnotationEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<LawAnnotationEntity> builder)
    {
        builder.ToTable("annotation");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.NodeId).HasColumnName("node_id").HasColumnType("uuid");
        builder.Property(e => e.Kind).HasConversion<string>().HasMaxLength(32).IsRequired().HasColumnName("kind");
        builder.Property(e => e.Ordinal).HasColumnName("ordinal");
        builder.Property(e => e.Body).HasColumnType("text").IsRequired().HasColumnName("body");
        builder.Property(e => e.RelatedCitation).HasMaxLength(500).HasColumnName("related_citation");
        builder.Property(e => e.RelatedNodeId).HasColumnName("related_node_id").HasColumnType("uuid");
        builder.Property(e => e.CreatedTimestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
        builder.HasOne(e => e.Node).WithMany(e => e.Annotations).HasForeignKey(e => e.NodeId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<LawNodeEntity>().WithMany().HasForeignKey(e => e.RelatedNodeId).OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(e => new { e.NodeId, e.Ordinal }).HasDatabaseName("ix_law_annotation_node_ordinal");
    }
}
