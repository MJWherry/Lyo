using Lyo.Search.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Law.Postgres.Database;

/// <summary>Maps <see cref="LawNodeEntity"/> to <c>law.node</c>.</summary>
public sealed class LawNodeEntityConfiguration : IEntityTypeConfiguration<LawNodeEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<LawNodeEntity> builder)
    {
        builder.ToTable("node");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.CodeId).HasColumnName("code_id").HasColumnType("uuid");
        builder.Property(e => e.ParentId).HasColumnName("parent_id").HasColumnType("uuid");
        builder.Property(e => e.Kind).HasConversion<string>().HasMaxLength(32).IsRequired().HasColumnName("kind");
        builder.Property(e => e.SortOrder).HasColumnName("sort_order");
        builder.Property(e => e.Label).HasMaxLength(64).IsRequired().HasColumnName("label");
        builder.Property(e => e.Heading).HasMaxLength(2000).IsRequired().HasColumnName("heading");
        builder.Property(e => e.Body).HasColumnType("text").HasColumnName("body");
        PostgresTextSearch.Map(builder, e => e.SearchVector, PostgresTextSearch.EnglishColumn("body"), "ix_law_node_search_vector");
        builder.Property(e => e.Citation).HasMaxLength(500).HasColumnName("citation");
        builder.Property(e => e.IsRepealed).HasColumnName("is_repealed");
        builder.Property(e => e.ContentSha256).HasMaxLength(64).IsRequired().HasColumnName("content_sha256");
        builder.Property(e => e.SourceKey).HasMaxLength(1000).IsRequired().HasColumnName("source_key");
        builder.Property(e => e.SourceUrl).HasMaxLength(2000).HasColumnName("source_url");
        builder.Property(e => e.CreatedTimestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
        builder.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
        builder.HasOne(e => e.Code).WithMany(e => e.Nodes).HasForeignKey(e => e.CodeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.Parent).WithMany(e => e.Children).HasForeignKey(e => e.ParentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(e => new { e.CodeId, e.SourceKey }).IsUnique().HasDatabaseName("ux_law_node_source_key");
        builder.HasIndex(e => new { e.CodeId, e.Citation }).IsUnique().HasFilter("\"citation\" IS NOT NULL").HasDatabaseName("ux_law_node_citation");
        builder.HasIndex(e => new { e.ParentId, e.SortOrder }).HasDatabaseName("ix_law_node_parent_sort");
    }
}
