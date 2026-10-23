using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Video.Postgres.Database;

public sealed class VideoGenreEntityConfiguration : IEntityTypeConfiguration<VideoGenreEntity>
{
    public void Configure(EntityTypeBuilder<VideoGenreEntity> builder)
    {
        builder.ToTable("genre");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.ParentGenreId).HasColumnName("parent_genre_id").HasColumnType("uuid");
        builder.Property(e => e.Slug).IsRequired().HasMaxLength(200).HasColumnName("slug");
        builder.Property(e => e.Name).IsRequired().HasMaxLength(200).HasColumnName("name");
        builder.Property(e => e.SortOrder).HasColumnName("sort_order");
        builder.HasIndex(e => e.Slug).IsUnique().HasDatabaseName("ix_video_genre_slug");
        builder.HasOne(e => e.Parent).WithMany().HasForeignKey(e => e.ParentGenreId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class VideoTitleGenreEntityConfiguration : IEntityTypeConfiguration<VideoTitleGenreEntity>
{
    public void Configure(EntityTypeBuilder<VideoTitleGenreEntity> builder)
    {
        builder.ToTable("title_genre");
        builder.HasKey(e => new { e.TitleId, e.GenreId });
        builder.Property(e => e.TitleId).HasColumnName("title_id").HasColumnType("uuid");
        builder.Property(e => e.GenreId).HasColumnName("genre_id").HasColumnType("uuid");
        builder.HasOne(e => e.Genre).WithMany(g => g.TitleLinks).HasForeignKey(e => e.GenreId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class VideoAlternateTitleEntityConfiguration : IEntityTypeConfiguration<VideoAlternateTitleEntity>
{
    public void Configure(EntityTypeBuilder<VideoAlternateTitleEntity> builder)
    {
        builder.ToTable("alternate_title");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.TitleId).HasColumnName("title_id").HasColumnType("uuid");
        builder.Property(e => e.Title).IsRequired().HasMaxLength(500).HasColumnName("title");
        builder.Property(e => e.Language).HasMaxLength(16).HasColumnName("language");
        builder.Property(e => e.Kind).HasConversion<string>().HasColumnType("text").HasColumnName("kind");
        builder.Property(e => e.SortOrder).HasColumnName("sort_order");
    }
}