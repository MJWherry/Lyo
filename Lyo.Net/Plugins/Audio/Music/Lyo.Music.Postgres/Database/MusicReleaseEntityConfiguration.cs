using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Music.Postgres.Database;

public sealed class MusicReleaseEntityConfiguration : IEntityTypeConfiguration<MusicReleaseEntity>
{
    public void Configure(EntityTypeBuilder<MusicReleaseEntity> builder)
    {
        builder.ToTable("release");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.Title).IsRequired().HasMaxLength(500).HasColumnName("title");
        builder.Property(e => e.Slug).IsRequired().HasMaxLength(500).HasColumnName("slug");
        builder.Property(e => e.ReleaseGroupId).HasColumnName("release_group_id").HasColumnType("uuid");
        builder.Property(e => e.Kind).HasConversion<string>().HasColumnType("text").HasColumnName("kind");
        builder.Property(e => e.Status).HasConversion<string>().HasColumnType("text").HasColumnName("status");
        builder.Property(e => e.ReleaseDate).HasColumnName("release_date");
        builder.Property(e => e.Label).HasMaxLength(500).HasColumnName("label");
        builder.Property(e => e.Language).HasMaxLength(16).HasColumnName("language");
        builder.Property(e => e.CoverImageRef).HasMaxLength(512).HasColumnName("cover_image_ref");
        builder.Property(e => e.PrimaryArtist).HasMaxLength(500).HasColumnName("primary_artist");
        builder.Property(e => e.FormatId).HasColumnName("format_id").HasColumnType("uuid");
        builder.Property(e => e.CreatedTimestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
        builder.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
        builder.HasIndex(e => e.Slug).IsUnique().HasDatabaseName("ix_music_release_slug");
        builder.HasIndex(e => e.PrimaryArtist).HasDatabaseName("ix_music_release_primary_artist");
        builder.HasMany(e => e.Identifiers).WithOne(i => i.Release).HasForeignKey(i => i.ReleaseId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(e => e.Credits).WithOne(c => c.Release).HasForeignKey(c => c.ReleaseId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(e => e.Tracks).WithOne(t => t.Release).HasForeignKey(t => t.ReleaseId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(e => e.GenreLinks).WithOne(g => g.Release).HasForeignKey(g => g.ReleaseId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Format).WithMany(f => f.Releases).HasForeignKey(e => e.FormatId).OnDelete(DeleteBehavior.Restrict);
    }
}
