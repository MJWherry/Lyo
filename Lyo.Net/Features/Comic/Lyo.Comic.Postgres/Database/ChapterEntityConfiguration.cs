using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Comic.Postgres.Database;

public sealed class ChapterEntityConfiguration : IEntityTypeConfiguration<ChapterEntity>
{
    public void Configure(EntityTypeBuilder<ChapterEntity> builder)
    {
        builder.ToTable("chapter");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.SeriesId).HasColumnName("series_id").HasColumnType("uuid");
        builder.Property(e => e.VolumeId).HasColumnName("volume_id").HasColumnType("uuid");
        builder.Property(e => e.ChapterNumber).IsRequired().HasColumnName("chapter_number").HasColumnType("numeric(8,2)");
        builder.Property(e => e.Title).HasMaxLength(500).HasColumnName("title");
        builder.Property(e => e.Language).IsRequired().HasMaxLength(10).HasColumnName("language");
        builder.Property(e => e.PageCount).HasColumnName("page_count");
        builder.Property(e => e.PublishedDate).HasColumnName("published_date");
        builder.Property(e => e.Source).HasMaxLength(512).HasColumnName("source");
        builder.Property(e => e.CreatedTimestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
        builder.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
        builder.HasIndex(e => e.SeriesId).HasDatabaseName("ix_comic_chapter_series");
        builder.HasIndex(e => e.VolumeId).HasDatabaseName("ix_comic_chapter_volume");
        // Unique per series + chapter number + language (one translation of each chapter)
        builder.HasIndex(e => new { e.SeriesId, e.ChapterNumber, e.Language }).IsUnique().HasDatabaseName("ix_comic_chapter_series_num_lang");
    }
}