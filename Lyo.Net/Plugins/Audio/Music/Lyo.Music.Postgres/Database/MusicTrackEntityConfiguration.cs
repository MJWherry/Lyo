using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Music.Postgres.Database;

public sealed class MusicTrackEntityConfiguration : IEntityTypeConfiguration<MusicTrackEntity>
{
    public const string SearchVectorSql = "to_tsvector('english', COALESCE(lyrics, ''))";

    public void Configure(EntityTypeBuilder<MusicTrackEntity> builder)
    {
        builder.ToTable("track");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.ReleaseId).HasColumnName("release_id").HasColumnType("uuid");
        builder.Property(e => e.DiscNumber).HasColumnName("disc_number");
        builder.Property(e => e.TrackNumber).HasColumnName("track_number");
        builder.Property(e => e.Title).IsRequired().HasMaxLength(500).HasColumnName("title");
        builder.Property(e => e.DurationSeconds).HasColumnName("duration_seconds");
        builder.Property(e => e.Isrc).HasMaxLength(32).HasColumnName("isrc");
        builder.Property(e => e.AudioFileRef).HasMaxLength(512).HasColumnName("audio_file_ref");
        builder.Property(e => e.RecordingId).HasColumnName("recording_id").HasColumnType("uuid");
        builder.Property(e => e.Lyrics).HasColumnType("text").HasColumnName("lyrics");
        builder.Property(e => e.SearchVector).HasColumnName("search_vector").HasColumnType("tsvector").HasComputedColumnSql(SearchVectorSql, true);
        builder.Property(e => e.CreatedTimestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
        builder.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
        builder.HasIndex(e => new { e.ReleaseId, e.DiscNumber, e.TrackNumber }).IsUnique().HasDatabaseName("ix_music_track_position");
        builder.HasIndex(e => e.Isrc).IsUnique().HasFilter("isrc IS NOT NULL").HasDatabaseName("ix_music_track_isrc");
        builder.HasIndex(e => e.SearchVector).HasMethod("GIN").HasDatabaseName("ix_music_track_search_vector");
        builder.HasOne(e => e.Recording).WithMany(r => r.Tracks).HasForeignKey(e => e.RecordingId).OnDelete(DeleteBehavior.SetNull);
        builder.HasMany(e => e.Credits).WithOne(c => c.Track).HasForeignKey(c => c.TrackId).OnDelete(DeleteBehavior.Cascade);
    }
}