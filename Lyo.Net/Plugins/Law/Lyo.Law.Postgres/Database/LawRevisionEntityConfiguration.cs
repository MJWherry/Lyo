using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Law.Postgres.Database;

/// <summary>Maps <see cref="LawRevisionEntity"/> to <c>law.revision</c>.</summary>
public sealed class LawRevisionEntityConfiguration : IEntityTypeConfiguration<LawRevisionEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<LawRevisionEntity> builder)
    {
        builder.ToTable("revision");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.NodeId).HasColumnName("node_id").HasColumnType("uuid");
        builder.Property(e => e.Revision).HasColumnName("revision");
        builder.Property(e => e.Heading).HasMaxLength(2000).IsRequired().HasColumnName("heading");
        builder.Property(e => e.Body).HasColumnType("text").HasColumnName("body");
        builder.Property(e => e.ContentSha256).HasMaxLength(64).IsRequired().HasColumnName("content_sha256");
        builder.Property(e => e.AnnotationsJson).HasColumnType("jsonb").IsRequired().HasColumnName("annotations_json");
        builder.Property(e => e.CreatedTimestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
        builder.HasOne(e => e.Node).WithMany().HasForeignKey(e => e.NodeId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => new { e.NodeId, e.Revision }).IsUnique().HasDatabaseName("ux_law_revision_node");
    }
}
