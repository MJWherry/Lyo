using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Video.Postgres.Database;

public sealed class VideoTitleEntityConfiguration : IEntityTypeConfiguration<VideoTitleEntity>
{
    public const string SearchVectorSql = "to_tsvector('english', COALESCE(transcript, ''))";

    public void Configure(EntityTypeBuilder<VideoTitleEntity> builder)
    {
        builder.ToTable("title");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.Title).IsRequired().HasMaxLength(500).HasColumnName("title");
        builder.Property(e => e.Slug).IsRequired().HasMaxLength(500).HasColumnName("slug");
        builder.Property(e => e.Kind).HasConversion<string>().HasColumnType("text").HasColumnName("kind");
        builder.Property(e => e.Status).HasConversion<string>().HasColumnType("text").HasColumnName("status");
        builder.Property(e => e.Year).HasColumnName("year");
        builder.Property(e => e.Language).HasMaxLength(16).HasColumnName("language");
        builder.Property(e => e.RuntimeSeconds).HasColumnName("runtime_seconds");
        builder.Property(e => e.CoverImageRef).HasMaxLength(512).HasColumnName("cover_image_ref");
        builder.Property(e => e.VideoFileRef).HasMaxLength(512).HasColumnName("video_file_ref");
        builder.Property(e => e.Overview).HasMaxLength(8192).HasColumnName("overview");
        builder.Property(e => e.Transcript).HasColumnType("text").HasColumnName("transcript");
        builder.Property(e => e.SearchVector).HasColumnName("search_vector").HasColumnType("tsvector").HasComputedColumnSql(SearchVectorSql, stored: true);
        builder.Property(e => e.CollectionId).HasColumnName("collection_id").HasColumnType("uuid");
        builder.Property(e => e.CreatedTimestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
        builder.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
        builder.HasIndex(e => e.Slug).IsUnique().HasDatabaseName("ix_video_title_slug");
        builder.HasIndex(e => e.SearchVector).HasMethod("GIN").HasDatabaseName("ix_video_title_search_vector");
        builder.HasMany(e => e.Credits).WithOne(c => c.Title).HasForeignKey(c => c.TitleId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(e => e.Identifiers).WithOne(i => i.Title).HasForeignKey(i => i.TitleId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(e => e.Seasons).WithOne(s => s.Title).HasForeignKey(s => s.TitleId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(e => e.GenreLinks).WithOne(g => g.Title).HasForeignKey(g => g.TitleId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(e => e.AlternateTitles).WithOne(a => a.Owner).HasForeignKey(a => a.TitleId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(e => e.Cuts).WithOne(c => c.Title).HasForeignKey(c => c.TitleId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Collection).WithMany(c => c.Titles).HasForeignKey(e => e.CollectionId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(e => e.Youtube).WithOne(y => y.Title).HasForeignKey<VideoYoutubeLinkEntity>(y => y.TitleId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class VideoSeasonEntityConfiguration : IEntityTypeConfiguration<VideoSeasonEntity>
{
    public void Configure(EntityTypeBuilder<VideoSeasonEntity> builder)
    {
        builder.ToTable("season");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.TitleId).HasColumnName("title_id").HasColumnType("uuid");
        builder.Property(e => e.SeasonNumber).HasColumnName("season_number");
        builder.Property(e => e.Name).HasMaxLength(500).HasColumnName("name");
        builder.Property(e => e.Year).HasColumnName("year");
        builder.HasIndex(e => new { e.TitleId, e.SeasonNumber }).IsUnique().HasDatabaseName("ix_video_season_number");
        builder.HasMany(e => e.Episodes).WithOne(ep => ep.Season).HasForeignKey(ep => ep.SeasonId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class VideoEpisodeEntityConfiguration : IEntityTypeConfiguration<VideoEpisodeEntity>
{
    public void Configure(EntityTypeBuilder<VideoEpisodeEntity> builder)
    {
        builder.ToTable("episode");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.SeasonId).HasColumnName("season_id").HasColumnType("uuid");
        builder.Property(e => e.EpisodeNumber).HasColumnName("episode_number");
        builder.Property(e => e.Title).IsRequired().HasMaxLength(500).HasColumnName("title");
        builder.Property(e => e.RuntimeSeconds).HasColumnName("runtime_seconds");
        builder.Property(e => e.AirDate).HasColumnName("air_date");
        builder.Property(e => e.VideoFileRef).HasMaxLength(512).HasColumnName("video_file_ref");
        builder.Property(e => e.StillImageRef).HasMaxLength(512).HasColumnName("still_image_ref");
        builder.Property(e => e.Transcript).HasColumnType("text").HasColumnName("transcript");
        builder.Property(e => e.SearchVector).HasColumnName("search_vector").HasColumnType("tsvector").HasComputedColumnSql(VideoTitleEntityConfiguration.SearchVectorSql, stored: true);
        builder.Property(e => e.CreatedTimestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
        builder.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
        builder.HasIndex(e => new { e.SeasonId, e.EpisodeNumber }).IsUnique().HasDatabaseName("ix_video_episode_number");
        builder.HasIndex(e => e.SearchVector).HasMethod("GIN").HasDatabaseName("ix_video_episode_search_vector");
        builder.HasMany(e => e.Credits).WithOne(c => c.Episode).HasForeignKey(c => c.EpisodeId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(e => e.Identifiers).WithOne(i => i.Episode).HasForeignKey(i => i.EpisodeId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Youtube).WithOne(y => y.Episode).HasForeignKey<VideoYoutubeLinkEntity>(y => y.EpisodeId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class VideoCreditEntityConfiguration : IEntityTypeConfiguration<VideoCreditEntity>
{
    public void Configure(EntityTypeBuilder<VideoCreditEntity> builder)
    {
        builder.ToTable("credit");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.TitleId).HasColumnName("title_id").HasColumnType("uuid");
        builder.Property(e => e.EpisodeId).HasColumnName("episode_id").HasColumnType("uuid");
        builder.Property(e => e.DisplayName).IsRequired().HasMaxLength(500).HasColumnName("display_name");
        builder.Property(e => e.SortName).HasMaxLength(500).HasColumnName("sort_name");
        builder.Property(e => e.Role).HasConversion<string>().HasColumnType("text").HasColumnName("role");
        builder.Property(e => e.Ordinal).HasColumnName("ordinal");
        builder.Property(e => e.CharacterName).HasMaxLength(500).HasColumnName("character_name");
        builder.Property(e => e.PersonRef).HasMaxLength(256).HasColumnName("person_ref");
        builder.HasIndex(e => new { e.TitleId, e.Role, e.Ordinal }).IsUnique().HasFilter("title_id IS NOT NULL").HasDatabaseName("ix_video_credit_title_role_ordinal");
        builder.HasIndex(e => new { e.EpisodeId, e.Role, e.Ordinal }).IsUnique().HasFilter("episode_id IS NOT NULL").HasDatabaseName("ix_video_credit_episode_role_ordinal");
    }
}

public sealed class VideoTitleIdentifierEntityConfiguration : IEntityTypeConfiguration<VideoTitleIdentifierEntity>
{
    public void Configure(EntityTypeBuilder<VideoTitleIdentifierEntity> builder)
    {
        builder.ToTable("title_identifier");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.TitleId).HasColumnName("title_id").HasColumnType("uuid");
        builder.Property(e => e.Scheme).IsRequired().HasMaxLength(50).HasColumnName("scheme");
        builder.Property(e => e.Value).IsRequired().HasMaxLength(256).HasColumnName("value");
        builder.Property(e => e.NormalizedValue).IsRequired().HasMaxLength(256).HasColumnName("normalized_value");
        builder.Property(e => e.IsPrimary).HasColumnName("is_primary");
        builder.HasIndex(e => new { e.Scheme, e.NormalizedValue }).IsUnique().HasDatabaseName("ix_video_title_identifier_scheme");
    }
}

public sealed class VideoEpisodeIdentifierEntityConfiguration : IEntityTypeConfiguration<VideoEpisodeIdentifierEntity>
{
    public void Configure(EntityTypeBuilder<VideoEpisodeIdentifierEntity> builder)
    {
        builder.ToTable("episode_identifier");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.EpisodeId).HasColumnName("episode_id").HasColumnType("uuid");
        builder.Property(e => e.Scheme).IsRequired().HasMaxLength(50).HasColumnName("scheme");
        builder.Property(e => e.Value).IsRequired().HasMaxLength(256).HasColumnName("value");
        builder.Property(e => e.NormalizedValue).IsRequired().HasMaxLength(256).HasColumnName("normalized_value");
        builder.Property(e => e.IsPrimary).HasColumnName("is_primary");
        builder.HasIndex(e => new { e.Scheme, e.NormalizedValue }).IsUnique().HasDatabaseName("ix_video_episode_identifier_scheme");
    }
}

public sealed class VideoYoutubeLinkEntityConfiguration : IEntityTypeConfiguration<VideoYoutubeLinkEntity>
{
    public void Configure(EntityTypeBuilder<VideoYoutubeLinkEntity> builder)
    {
        builder.ToTable("youtube_link");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.TitleId).HasColumnName("title_id").HasColumnType("uuid");
        builder.Property(e => e.EpisodeId).HasColumnName("episode_id").HasColumnType("uuid");
        builder.Property(e => e.VideoId).IsRequired().HasMaxLength(16).HasColumnName("video_id");
        builder.Property(e => e.ChannelId).IsRequired().HasMaxLength(32).HasColumnName("channel_id");
        builder.Property(e => e.PlaylistId).HasMaxLength(64).HasColumnName("playlist_id");
        builder.HasIndex(e => e.VideoId).IsUnique().HasDatabaseName("ix_video_youtube_video_id");
        builder.HasIndex(e => e.TitleId).IsUnique().HasFilter("title_id IS NOT NULL").HasDatabaseName("ix_video_youtube_title");
        builder.HasIndex(e => e.EpisodeId).IsUnique().HasFilter("episode_id IS NOT NULL").HasDatabaseName("ix_video_youtube_episode");
    }
}
