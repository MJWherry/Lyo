using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Audiobook.Postgres.Database;

public sealed class AudiobookWorkEntityConfiguration : IEntityTypeConfiguration<AudiobookWorkEntity>
{
    public const string SearchVectorSql = "to_tsvector('english', COALESCE(transcript, ''))";

    public void Configure(EntityTypeBuilder<AudiobookWorkEntity> builder)
    {
        builder.ToTable("work");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.Title).IsRequired().HasMaxLength(500).HasColumnName("title");
        builder.Property(e => e.Subtitle).HasMaxLength(500).HasColumnName("subtitle");
        builder.Property(e => e.Slug).IsRequired().HasMaxLength(500).HasColumnName("slug");
        builder.Property(e => e.Language).HasMaxLength(16).HasColumnName("language");
        builder.Property(e => e.Year).HasColumnName("year");
        builder.Property(e => e.RuntimeSeconds).HasColumnName("runtime_seconds");
        builder.Property(e => e.CoverImageRef).HasMaxLength(512).HasColumnName("cover_image_ref");
        builder.Property(e => e.AudioFileRef).HasMaxLength(512).HasColumnName("audio_file_ref");
        builder.Property(e => e.Overview).HasMaxLength(8192).HasColumnName("overview");
        builder.Property(e => e.SeriesId).HasColumnName("series_id").HasColumnType("uuid");
        builder.Property(e => e.SeriesIndex).HasColumnType("numeric").HasColumnName("series_index");
        builder.Property(e => e.Transcript).HasColumnType("text").HasColumnName("transcript");
        builder.Property(e => e.SearchVector).HasColumnName("search_vector").HasColumnType("tsvector").HasComputedColumnSql(SearchVectorSql, stored: true);
        builder.Property(e => e.CreatedTimestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
        builder.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
        builder.HasIndex(e => e.Slug).IsUnique().HasDatabaseName("ix_audiobook_work_slug");
        builder.HasIndex(e => e.SearchVector).HasMethod("GIN").HasDatabaseName("ix_audiobook_work_search_vector");
        builder.HasMany(e => e.Credits).WithOne(c => c.Work).HasForeignKey(c => c.WorkId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(e => e.Identifiers).WithOne(i => i.Work).HasForeignKey(i => i.WorkId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(e => e.Chapters).WithOne(c => c.Work).HasForeignKey(c => c.WorkId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(e => e.GenreLinks).WithOne(g => g.Work).HasForeignKey(g => g.WorkId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Series).WithMany(s => s.Works).HasForeignKey(e => e.SeriesId).OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class AudiobookChapterEntityConfiguration : IEntityTypeConfiguration<AudiobookChapterEntity>
{
    public void Configure(EntityTypeBuilder<AudiobookChapterEntity> builder)
    {
        builder.ToTable("chapter");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.WorkId).HasColumnName("work_id").HasColumnType("uuid");
        builder.Property(e => e.Number).HasColumnName("number");
        builder.Property(e => e.Title).IsRequired().HasMaxLength(500).HasColumnName("title");
        builder.Property(e => e.StartSeconds).HasColumnName("start_seconds");
        builder.Property(e => e.DurationSeconds).HasColumnName("duration_seconds");
        builder.Property(e => e.AudioFileRef).HasMaxLength(512).HasColumnName("audio_file_ref");
        builder.Property(e => e.Transcript).HasColumnType("text").HasColumnName("transcript");
        builder.Property(e => e.SearchVector).HasColumnName("search_vector").HasColumnType("tsvector").HasComputedColumnSql(AudiobookWorkEntityConfiguration.SearchVectorSql, stored: true);
        builder.Property(e => e.CreatedTimestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
        builder.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
        builder.HasIndex(e => new { e.WorkId, e.Number }).IsUnique().HasDatabaseName("ix_audiobook_chapter_number");
        builder.HasIndex(e => e.SearchVector).HasMethod("GIN").HasDatabaseName("ix_audiobook_chapter_search_vector");
    }
}

public sealed class AudiobookCreditEntityConfiguration : IEntityTypeConfiguration<AudiobookCreditEntity>
{
    public void Configure(EntityTypeBuilder<AudiobookCreditEntity> builder)
    {
        builder.ToTable("credit");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.WorkId).HasColumnName("work_id").HasColumnType("uuid");
        builder.Property(e => e.DisplayName).IsRequired().HasMaxLength(500).HasColumnName("display_name");
        builder.Property(e => e.SortName).HasMaxLength(500).HasColumnName("sort_name");
        builder.Property(e => e.Role).HasConversion<string>().HasColumnType("text").HasColumnName("role");
        builder.Property(e => e.Ordinal).HasColumnName("ordinal");
        builder.Property(e => e.PersonRef).HasMaxLength(256).HasColumnName("person_ref");
        builder.HasIndex(e => new { e.WorkId, e.Role, e.Ordinal }).IsUnique().HasDatabaseName("ix_audiobook_credit_role_ordinal");
    }
}

public sealed class AudiobookIdentifierEntityConfiguration : IEntityTypeConfiguration<AudiobookIdentifierEntity>
{
    public void Configure(EntityTypeBuilder<AudiobookIdentifierEntity> builder)
    {
        builder.ToTable("work_identifier");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.WorkId).HasColumnName("work_id").HasColumnType("uuid");
        builder.Property(e => e.Scheme).IsRequired().HasMaxLength(50).HasColumnName("scheme");
        builder.Property(e => e.Value).IsRequired().HasMaxLength(256).HasColumnName("value");
        builder.Property(e => e.NormalizedValue).IsRequired().HasMaxLength(256).HasColumnName("normalized_value");
        builder.Property(e => e.IsPrimary).HasColumnName("is_primary");
        builder.HasIndex(e => new { e.Scheme, e.NormalizedValue }).IsUnique().HasDatabaseName("ix_audiobook_identifier_scheme");
    }
}

public sealed class AudiobookSeriesEntityConfiguration : IEntityTypeConfiguration<AudiobookSeriesEntity>
{
    public void Configure(EntityTypeBuilder<AudiobookSeriesEntity> builder)
    {
        builder.ToTable("series");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.Title).IsRequired().HasMaxLength(500).HasColumnName("title");
        builder.Property(e => e.Slug).IsRequired().HasMaxLength(500).HasColumnName("slug");
        builder.HasIndex(e => e.Slug).IsUnique().HasDatabaseName("ix_audiobook_series_slug");
    }
}

public sealed class AudiobookGenreEntityConfiguration : IEntityTypeConfiguration<AudiobookGenreEntity>
{
    public void Configure(EntityTypeBuilder<AudiobookGenreEntity> builder)
    {
        builder.ToTable("genre");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.ParentGenreId).HasColumnName("parent_genre_id").HasColumnType("uuid");
        builder.Property(e => e.Slug).IsRequired().HasMaxLength(200).HasColumnName("slug");
        builder.Property(e => e.Name).IsRequired().HasMaxLength(200).HasColumnName("name");
        builder.Property(e => e.SortOrder).HasColumnName("sort_order");
        builder.HasIndex(e => e.Slug).IsUnique().HasDatabaseName("ix_audiobook_genre_slug");
        builder.HasOne(e => e.Parent).WithMany().HasForeignKey(e => e.ParentGenreId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AudiobookWorkGenreEntityConfiguration : IEntityTypeConfiguration<AudiobookWorkGenreEntity>
{
    public void Configure(EntityTypeBuilder<AudiobookWorkGenreEntity> builder)
    {
        builder.ToTable("work_genre");
        builder.HasKey(e => new { e.WorkId, e.GenreId });
        builder.Property(e => e.WorkId).HasColumnName("work_id").HasColumnType("uuid");
        builder.Property(e => e.GenreId).HasColumnName("genre_id").HasColumnType("uuid");
        builder.HasOne(e => e.Genre).WithMany(g => g.WorkLinks).HasForeignKey(e => e.GenreId).OnDelete(DeleteBehavior.Restrict);
    }
}
