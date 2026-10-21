using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Podcast.Postgres.Database;

public sealed class PodcastShowEntityConfiguration : IEntityTypeConfiguration<PodcastShowEntity>
{
    public void Configure(EntityTypeBuilder<PodcastShowEntity> builder)
    {
        builder.ToTable("show");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.Title).IsRequired().HasMaxLength(500).HasColumnName("title");
        builder.Property(e => e.Subtitle).HasMaxLength(500).HasColumnName("subtitle");
        builder.Property(e => e.Slug).IsRequired().HasMaxLength(500).HasColumnName("slug");
        builder.Property(e => e.Status).HasConversion<string>().HasColumnType("text").HasColumnName("status");
        builder.Property(e => e.Language).HasMaxLength(16).HasColumnName("language");
        builder.Property(e => e.CoverImageRef).HasMaxLength(512).HasColumnName("cover_image_ref");
        builder.Property(e => e.Overview).HasMaxLength(8192).HasColumnName("overview");
        builder.Property(e => e.CreatedTimestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
        builder.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
        builder.HasIndex(e => e.Slug).IsUnique().HasDatabaseName("ix_podcast_show_slug");
        builder.HasMany(e => e.Credits).WithOne(c => c.Show).HasForeignKey(c => c.ShowId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(e => e.Identifiers).WithOne(i => i.Show).HasForeignKey(i => i.ShowId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(e => e.Seasons).WithOne(s => s.Show).HasForeignKey(s => s.ShowId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(e => e.GenreLinks).WithOne(g => g.Show).HasForeignKey(g => g.ShowId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class PodcastSeasonEntityConfiguration : IEntityTypeConfiguration<PodcastSeasonEntity>
{
    public void Configure(EntityTypeBuilder<PodcastSeasonEntity> builder)
    {
        builder.ToTable("season");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.ShowId).HasColumnName("show_id").HasColumnType("uuid");
        builder.Property(e => e.SeasonNumber).HasColumnName("season_number");
        builder.Property(e => e.Name).HasMaxLength(500).HasColumnName("name");
        builder.Property(e => e.Year).HasColumnName("year");
        builder.HasIndex(e => new { e.ShowId, e.SeasonNumber }).IsUnique().HasDatabaseName("ix_podcast_season_number");
        builder.HasMany(e => e.Episodes).WithOne(ep => ep.Season).HasForeignKey(ep => ep.SeasonId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class PodcastEpisodeEntityConfiguration : IEntityTypeConfiguration<PodcastEpisodeEntity>
{
    public const string SearchVectorSql = "to_tsvector('english', COALESCE(transcript, ''))";

    public void Configure(EntityTypeBuilder<PodcastEpisodeEntity> builder)
    {
        builder.ToTable("episode");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.SeasonId).HasColumnName("season_id").HasColumnType("uuid");
        builder.Property(e => e.EpisodeNumber).HasColumnName("episode_number");
        builder.Property(e => e.Title).IsRequired().HasMaxLength(500).HasColumnName("title");
        builder.Property(e => e.RuntimeSeconds).HasColumnName("runtime_seconds");
        builder.Property(e => e.PublishedDate).HasColumnName("published_date");
        builder.Property(e => e.AudioFileRef).HasMaxLength(512).HasColumnName("audio_file_ref");
        builder.Property(e => e.Description).HasMaxLength(8192).HasColumnName("description");
        builder.Property(e => e.Transcript).HasColumnType("text").HasColumnName("transcript");
        builder.Property(e => e.SearchVector).HasColumnName("search_vector").HasColumnType("tsvector").HasComputedColumnSql(SearchVectorSql, true);
        builder.Property(e => e.CreatedTimestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
        builder.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
        builder.HasIndex(e => new { e.SeasonId, e.EpisodeNumber }).IsUnique().HasDatabaseName("ix_podcast_episode_number");
        builder.HasIndex(e => e.SearchVector).HasMethod("GIN").HasDatabaseName("ix_podcast_episode_search_vector");
        builder.HasMany(e => e.Credits).WithOne(c => c.Episode).HasForeignKey(c => c.EpisodeId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(e => e.Identifiers).WithOne(i => i.Episode).HasForeignKey(i => i.EpisodeId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class PodcastCreditEntityConfiguration : IEntityTypeConfiguration<PodcastCreditEntity>
{
    public void Configure(EntityTypeBuilder<PodcastCreditEntity> builder)
    {
        builder.ToTable("credit");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.ShowId).HasColumnName("show_id").HasColumnType("uuid");
        builder.Property(e => e.EpisodeId).HasColumnName("episode_id").HasColumnType("uuid");
        builder.Property(e => e.DisplayName).IsRequired().HasMaxLength(500).HasColumnName("display_name");
        builder.Property(e => e.SortName).HasMaxLength(500).HasColumnName("sort_name");
        builder.Property(e => e.Role).HasConversion<string>().HasColumnType("text").HasColumnName("role");
        builder.Property(e => e.Ordinal).HasColumnName("ordinal");
        builder.Property(e => e.PersonRef).HasMaxLength(256).HasColumnName("person_ref");
        builder.HasIndex(e => new { e.ShowId, e.Role, e.Ordinal }).IsUnique().HasFilter("show_id IS NOT NULL").HasDatabaseName("ix_podcast_credit_show_role_ordinal");
        builder.HasIndex(e => new { e.EpisodeId, e.Role, e.Ordinal }).IsUnique().HasFilter("episode_id IS NOT NULL").HasDatabaseName("ix_podcast_credit_episode_role_ordinal");
    }
}

public sealed class PodcastShowIdentifierEntityConfiguration : IEntityTypeConfiguration<PodcastShowIdentifierEntity>
{
    public void Configure(EntityTypeBuilder<PodcastShowIdentifierEntity> builder)
    {
        builder.ToTable("show_identifier");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.ShowId).HasColumnName("show_id").HasColumnType("uuid");
        builder.Property(e => e.Scheme).IsRequired().HasMaxLength(50).HasColumnName("scheme");
        builder.Property(e => e.Value).IsRequired().HasMaxLength(256).HasColumnName("value");
        builder.Property(e => e.NormalizedValue).IsRequired().HasMaxLength(256).HasColumnName("normalized_value");
        builder.Property(e => e.IsPrimary).HasColumnName("is_primary");
        builder.HasIndex(e => new { e.Scheme, e.NormalizedValue }).IsUnique().HasDatabaseName("ix_podcast_show_identifier_scheme");
    }
}

public sealed class PodcastEpisodeIdentifierEntityConfiguration : IEntityTypeConfiguration<PodcastEpisodeIdentifierEntity>
{
    public void Configure(EntityTypeBuilder<PodcastEpisodeIdentifierEntity> builder)
    {
        builder.ToTable("episode_identifier");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.EpisodeId).HasColumnName("episode_id").HasColumnType("uuid");
        builder.Property(e => e.Scheme).IsRequired().HasMaxLength(50).HasColumnName("scheme");
        builder.Property(e => e.Value).IsRequired().HasMaxLength(256).HasColumnName("value");
        builder.Property(e => e.NormalizedValue).IsRequired().HasMaxLength(256).HasColumnName("normalized_value");
        builder.Property(e => e.IsPrimary).HasColumnName("is_primary");
        builder.HasIndex(e => new { e.Scheme, e.NormalizedValue }).IsUnique().HasDatabaseName("ix_podcast_episode_identifier_scheme");
    }
}

public sealed class PodcastGenreEntityConfiguration : IEntityTypeConfiguration<PodcastGenreEntity>
{
    public void Configure(EntityTypeBuilder<PodcastGenreEntity> builder)
    {
        builder.ToTable("genre");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.ParentGenreId).HasColumnName("parent_genre_id").HasColumnType("uuid");
        builder.Property(e => e.Slug).IsRequired().HasMaxLength(200).HasColumnName("slug");
        builder.Property(e => e.Name).IsRequired().HasMaxLength(200).HasColumnName("name");
        builder.Property(e => e.SortOrder).HasColumnName("sort_order");
        builder.HasIndex(e => e.Slug).IsUnique().HasDatabaseName("ix_podcast_genre_slug");
        builder.HasOne(e => e.Parent).WithMany().HasForeignKey(e => e.ParentGenreId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PodcastShowGenreEntityConfiguration : IEntityTypeConfiguration<PodcastShowGenreEntity>
{
    public void Configure(EntityTypeBuilder<PodcastShowGenreEntity> builder)
    {
        builder.ToTable("show_genre");
        builder.HasKey(e => new { e.ShowId, e.GenreId });
        builder.Property(e => e.ShowId).HasColumnName("show_id").HasColumnType("uuid");
        builder.Property(e => e.GenreId).HasColumnName("genre_id").HasColumnType("uuid");
        builder.HasOne(e => e.Genre).WithMany(g => g.ShowLinks).HasForeignKey(e => e.GenreId).OnDelete(DeleteBehavior.Restrict);
    }
}