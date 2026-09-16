using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Book.Postgres.Database;

public sealed class BookSeriesEntityConfiguration : IEntityTypeConfiguration<BookSeriesEntity>
{
    public void Configure(EntityTypeBuilder<BookSeriesEntity> builder)
    {
        builder.ToTable("series");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.Title).IsRequired().HasMaxLength(500).HasColumnName("title");
        builder.Property(e => e.Slug).IsRequired().HasMaxLength(500).HasColumnName("slug");
        builder.Property(e => e.Subtitle).HasMaxLength(500).HasColumnName("subtitle");
        builder.Property(e => e.Description).HasMaxLength(8192).HasColumnName("description");
        builder.Property(e => e.Language).HasMaxLength(16).HasColumnName("language");
        builder.Property(e => e.OriginalLanguage).HasMaxLength(16).HasColumnName("original_language");
        builder.Property(e => e.Publisher).HasMaxLength(500).HasColumnName("publisher");
        builder.Property(e => e.Imprint).HasMaxLength(500).HasColumnName("imprint");
        builder.Property(e => e.Status).HasConversion<string>().HasColumnType("text").HasColumnName("status");
        builder.Property(e => e.PlannedBookCount).HasColumnName("planned_book_count");
        builder.Property(e => e.StartedDate).HasColumnName("started_date");
        builder.Property(e => e.EndedDate).HasColumnName("ended_date");
        builder.Property(e => e.CoverImageRef).HasMaxLength(512).HasColumnName("cover_image_ref");
        builder.Property(e => e.Source).HasMaxLength(512).HasColumnName("source");
        builder.Property(e => e.CreatedTimestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
        builder.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
        builder.HasIndex(e => e.Slug).IsUnique().HasDatabaseName("ix_book_series_slug");
        builder.HasMany(e => e.AlternateTitles).WithOne(a => a.Series).HasForeignKey(a => a.SeriesId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(e => e.Books).WithOne(b => b.Series).HasForeignKey(b => b.SeriesId).OnDelete(DeleteBehavior.SetNull);
    }
}
