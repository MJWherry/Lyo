using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Comic.Postgres.Database;

public sealed class VolumeEntityConfiguration : IEntityTypeConfiguration<VolumeEntity>
{
    public void Configure(EntityTypeBuilder<VolumeEntity> builder)
    {
        builder.ToTable("volume");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.SeriesId).HasColumnName("series_id").HasColumnType("uuid");
        builder.Property(e => e.VolumeNumber).HasColumnName("volume_number").HasColumnType("numeric(8,2)");
        builder.Property(e => e.Title).HasMaxLength(500).HasColumnName("title");
        builder.Property(e => e.CoverImageRef).HasColumnName("cover_image_ref");
        builder.Property(e => e.PublishedDate).HasColumnName("published_date");
        builder.Property(e => e.CreatedTimestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
        builder.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
        builder.HasIndex(e => e.SeriesId).HasDatabaseName("ix_comic_volume_series");
        // Partial unique index enforced at app level; EF doesn't support WHERE clauses here directly
        builder.HasIndex(e => new { e.SeriesId, e.VolumeNumber }).HasDatabaseName("ix_comic_volume_series_number");
        builder.HasMany(e => e.Chapters).WithOne(c => c.Volume).HasForeignKey(c => c.VolumeId).OnDelete(DeleteBehavior.SetNull);
    }
}