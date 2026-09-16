using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Music.Postgres.Database;

public sealed class MusicGenreEntityConfiguration : IEntityTypeConfiguration<MusicGenreEntity>
{
    public void Configure(EntityTypeBuilder<MusicGenreEntity> builder)
    {
        builder.ToTable("genre");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.ParentGenreId).HasColumnName("parent_genre_id").HasColumnType("uuid");
        builder.Property(e => e.Slug).IsRequired().HasMaxLength(200).HasColumnName("slug");
        builder.Property(e => e.Name).IsRequired().HasMaxLength(200).HasColumnName("name");
        builder.Property(e => e.SortOrder).HasColumnName("sort_order");
        builder.HasIndex(e => e.Slug).IsUnique().HasDatabaseName("ix_music_genre_slug");
        builder.HasOne(e => e.Parent).WithMany().HasForeignKey(e => e.ParentGenreId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class MusicFormatEntityConfiguration : IEntityTypeConfiguration<MusicFormatEntity>
{
    public void Configure(EntityTypeBuilder<MusicFormatEntity> builder)
    {
        builder.ToTable("format");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.Slug).IsRequired().HasMaxLength(200).HasColumnName("slug");
        builder.Property(e => e.Name).IsRequired().HasMaxLength(200).HasColumnName("name");
        builder.Property(e => e.SortOrder).HasColumnName("sort_order");
        builder.HasIndex(e => e.Slug).IsUnique().HasDatabaseName("ix_music_format_slug");
    }
}

public sealed class MusicReleaseGenreEntityConfiguration : IEntityTypeConfiguration<MusicReleaseGenreEntity>
{
    public void Configure(EntityTypeBuilder<MusicReleaseGenreEntity> builder)
    {
        builder.ToTable("release_genre");
        builder.HasKey(e => new { e.ReleaseId, e.GenreId });
        builder.Property(e => e.ReleaseId).HasColumnName("release_id").HasColumnType("uuid");
        builder.Property(e => e.GenreId).HasColumnName("genre_id").HasColumnType("uuid");
        builder.HasOne(e => e.Genre).WithMany(g => g.ReleaseLinks).HasForeignKey(e => e.GenreId).OnDelete(DeleteBehavior.Restrict);
    }
}
