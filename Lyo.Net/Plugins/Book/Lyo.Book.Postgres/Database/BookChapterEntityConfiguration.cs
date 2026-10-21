using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Book.Postgres.Database;

public sealed class BookChapterEntityConfiguration : IEntityTypeConfiguration<BookChapterEntity>
{
    public void Configure(EntityTypeBuilder<BookChapterEntity> builder)
    {
        builder.ToTable("chapter");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.BookId).HasColumnName("book_id").HasColumnType("uuid");
        builder.Property(e => e.ChapterNumber).HasColumnType("numeric").HasColumnName("chapter_number");
        builder.Property(e => e.Title).HasMaxLength(500).HasColumnName("title");
        builder.Property(e => e.Subtitle).HasMaxLength(500).HasColumnName("subtitle");
        builder.Property(e => e.Language).IsRequired().HasMaxLength(16).HasColumnName("language");
        builder.Property(e => e.SortOrder).HasColumnName("sort_order");
        builder.Property(e => e.StartPrintPage).HasColumnName("start_print_page");
        builder.Property(e => e.EndPrintPage).HasColumnName("end_print_page");
        builder.Property(e => e.PageCount).HasColumnName("page_count");
        builder.Property(e => e.WordCount).HasColumnName("word_count");
        builder.Property(e => e.Summary).HasMaxLength(8192).HasColumnName("summary");
        builder.Property(e => e.PublishedDate).HasColumnName("published_date");
        builder.Property(e => e.CoverImageRef).HasMaxLength(512).HasColumnName("cover_image_ref");
        builder.Property(e => e.Source).HasMaxLength(512).HasColumnName("source");
        builder.Property(e => e.CreatedTimestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
        builder.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
        builder.HasIndex(e => new { e.BookId, e.ChapterNumber, e.Language }).IsUnique().HasDatabaseName("ix_book_chapter_book_number_language");
        builder.HasMany(e => e.Pages).WithOne(p => p.Chapter).HasForeignKey(p => p.ChapterId).OnDelete(DeleteBehavior.Cascade);
    }
}