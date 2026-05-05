using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Comic.Postgres.Database;

public sealed class SeriesEntityConfiguration : IEntityTypeConfiguration<SeriesEntity>
{
    public void Configure(EntityTypeBuilder<SeriesEntity> builder)
    {
        builder.ToTable("series");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.Title).IsRequired().HasMaxLength(500).HasColumnName("title");
        builder.Property(e => e.Slug).IsRequired().HasMaxLength(500).HasColumnName("slug");
        builder.Property(e => e.ComicType).HasConversion<string>().HasColumnType("text").HasColumnName("comic_type");
        builder.Property(e => e.Status).HasConversion<string>().HasColumnType("text").HasColumnName("status");
        builder.Property(e => e.Description).HasMaxLength(8192).HasColumnName("description");
        builder.Property(e => e.Language).HasMaxLength(10).HasColumnName("language");
        builder.Property(e => e.PublishedYear).HasColumnName("published_year");
        builder.Property(e => e.Author).HasMaxLength(500).HasColumnName("author");
        builder.Property(e => e.Artist).HasMaxLength(500).HasColumnName("artist");
        builder.Property(e => e.Publisher).HasMaxLength(500).HasColumnName("publisher");
        builder.Property(e => e.Source).HasMaxLength(512).HasColumnName("source");
        builder.Property(e => e.CoverImageRef).HasMaxLength(512).HasColumnName("cover_image_ref");
        builder.Property(e => e.Demographic).HasMaxLength(50).HasColumnName("demographic");
        builder.Property(e => e.CreatedTimestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
        builder.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
        builder.HasIndex(e => e.Slug).IsUnique().HasDatabaseName("ix_comic_series_slug");
        builder.HasIndex(e => e.ComicType).HasDatabaseName("ix_comic_series_type");
        builder.HasIndex(e => e.Status).HasDatabaseName("ix_comic_series_status");
        builder.HasMany(e => e.AlternateTitles).WithOne(a => a.Series).HasForeignKey(a => a.SeriesId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(e => e.Volumes).WithOne(v => v.Series).HasForeignKey(v => v.SeriesId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(e => e.Chapters).WithOne(c => c.Series).HasForeignKey(c => c.SeriesId).OnDelete(DeleteBehavior.Cascade);
    }
}