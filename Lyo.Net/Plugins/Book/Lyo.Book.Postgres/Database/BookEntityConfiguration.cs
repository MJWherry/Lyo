using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Book.Postgres.Database;

public sealed class BookEntityConfiguration : IEntityTypeConfiguration<BookEntity>
{
    public void Configure(EntityTypeBuilder<BookEntity> builder)
    {
        builder.ToTable("book");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.Title).IsRequired().HasMaxLength(500).HasColumnName("title");
        builder.Property(e => e.Subtitle).HasMaxLength(500).HasColumnName("subtitle");
        builder.Property(e => e.Slug).IsRequired().HasMaxLength(500).HasColumnName("slug");
        builder.Property(e => e.SeriesId).HasColumnName("series_id").HasColumnType("uuid");
        builder.Property(e => e.SeriesIndex).HasColumnType("numeric").HasColumnName("series_index");
        builder.Property(e => e.PrimaryAuthor).HasMaxLength(500).HasColumnName("primary_author");
        builder.Property(e => e.TypeId).HasColumnName("type_id").HasColumnType("uuid");
        builder.Property(e => e.FormatId).HasColumnName("format_id").HasColumnType("uuid");
        builder.Property(e => e.Status).HasConversion<string>().HasColumnType("text").HasColumnName("status");
        builder.Property(e => e.Description).HasMaxLength(8192).HasColumnName("description");
        builder.Property(e => e.Language).HasMaxLength(16).HasColumnName("language");
        builder.Property(e => e.OriginalLanguage).HasMaxLength(16).HasColumnName("original_language");
        builder.Property(e => e.PublishedYear).HasColumnName("published_year");
        builder.Property(e => e.PublishedDate).HasColumnName("published_date");
        builder.Property(e => e.Publisher).HasMaxLength(500).HasColumnName("publisher");
        builder.Property(e => e.Imprint).HasMaxLength(500).HasColumnName("imprint");
        builder.Property(e => e.EditionStatement).HasMaxLength(200).HasColumnName("edition_statement");
        builder.Property(e => e.EditionNumber).HasColumnName("edition_number");
        builder.Property(e => e.PrintPageCount).HasColumnName("print_page_count");
        builder.Property(e => e.WordCount).HasColumnName("word_count");
        builder.Property(e => e.Audience).HasMaxLength(100).HasColumnName("audience");
        builder.Property(e => e.ContentRating).HasMaxLength(50).HasColumnName("content_rating");
        builder.Property(e => e.CountryOfOrigin).HasMaxLength(100).HasColumnName("country_of_origin");
        builder.Property(e => e.CoverImageRef).HasMaxLength(512).HasColumnName("cover_image_ref");
        builder.Property(e => e.Source).HasMaxLength(512).HasColumnName("source");
        builder.Property(e => e.CreatedTimestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
        builder.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
        builder.HasIndex(e => e.Slug).IsUnique().HasDatabaseName("ix_book_book_slug");
        builder.HasIndex(e => e.PrimaryAuthor).HasDatabaseName("ix_book_book_primary_author");
        builder.HasOne(e => e.Type).WithMany(t => t.Books).HasForeignKey(e => e.TypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.Format).WithMany(f => f.Books).HasForeignKey(e => e.FormatId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(e => e.AlternateTitles).WithOne(a => a.Book).HasForeignKey(a => a.BookId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(e => e.Identifiers).WithOne(i => i.Book).HasForeignKey(i => i.BookId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(e => e.Contributors).WithOne(c => c.Book).HasForeignKey(c => c.BookId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(e => e.GenreLinks).WithOne(g => g.Book).HasForeignKey(g => g.BookId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(e => e.Chapters).WithOne(c => c.Book).HasForeignKey(c => c.BookId).OnDelete(DeleteBehavior.Cascade);
    }
}