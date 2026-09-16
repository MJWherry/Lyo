using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Video.Postgres.Database;

public sealed class VideoCollectionEntityConfiguration : IEntityTypeConfiguration<VideoCollectionEntity>
{
    public void Configure(EntityTypeBuilder<VideoCollectionEntity> builder)
    {
        builder.ToTable("collection");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.Title).IsRequired().HasMaxLength(500).HasColumnName("title");
        builder.Property(e => e.Slug).IsRequired().HasMaxLength(500).HasColumnName("slug");
        builder.HasIndex(e => e.Slug).IsUnique().HasDatabaseName("ix_video_collection_slug");
    }
}

public sealed class VideoCutEntityConfiguration : IEntityTypeConfiguration<VideoCutEntity>
{
    public void Configure(EntityTypeBuilder<VideoCutEntity> builder)
    {
        builder.ToTable("cut");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.TitleId).HasColumnName("title_id").HasColumnType("uuid");
        builder.Property(e => e.Name).IsRequired().HasMaxLength(200).HasColumnName("name");
        builder.Property(e => e.RuntimeSeconds).HasColumnName("runtime_seconds");
        builder.Property(e => e.VideoFileRef).HasMaxLength(512).HasColumnName("video_file_ref");
        builder.Property(e => e.IsPrimary).HasColumnName("is_primary");
        builder.HasIndex(e => e.TitleId).IsUnique().HasFilter("is_primary").HasDatabaseName("ix_video_cut_primary");
    }
}
