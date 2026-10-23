using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Book.Postgres.Database;

public sealed class BookPageEntityConfiguration : IEntityTypeConfiguration<BookPageEntity>
{
    public const string SearchVectorSql =
        "to_tsvector('english', CASE WHEN content_type = 'text/html' THEN regexp_replace(COALESCE(text, ''), '<[^>]+>', ' ', 'g') ELSE COALESCE(text, '') END)";

    public void Configure(EntityTypeBuilder<BookPageEntity> builder)
    {
        builder.ToTable("page");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.ChapterId).HasColumnName("chapter_id").HasColumnType("uuid");
        builder.Property(e => e.PageNumber).HasColumnName("page_number");
        builder.Property(e => e.PrintPageLabel).HasMaxLength(32).HasColumnName("print_page_label");
        builder.Property(e => e.Text).HasColumnType("text").HasColumnName("text");
        builder.Property(e => e.SearchVector).HasColumnName("search_vector").HasColumnType("tsvector").HasComputedColumnSql(SearchVectorSql, true);
        builder.Property(e => e.ContentType).IsRequired().HasMaxLength(64).HasColumnName("content_type");
        builder.Property(e => e.CharacterCount).HasColumnName("character_count");
        builder.Property(e => e.WordCount).HasColumnName("word_count");
        builder.Property(e => e.ScanFileRef).HasMaxLength(512).HasColumnName("scan_file_ref");
        builder.Property(e => e.Width).HasColumnName("width");
        builder.Property(e => e.Height).HasColumnName("height");
        builder.Property(e => e.CreatedTimestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
        builder.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
        builder.HasIndex(e => new { e.ChapterId, e.PageNumber }).IsUnique().HasDatabaseName("ix_book_page_chapter_number");
        builder.HasIndex(e => e.SearchVector).HasMethod("GIN").HasDatabaseName("ix_book_page_search_vector");
    }
}