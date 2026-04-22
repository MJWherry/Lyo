using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Comic.Postgres.Database;

public sealed class AlternateTitleEntityConfiguration : IEntityTypeConfiguration<AlternateTitleEntity>
{
    public void Configure(EntityTypeBuilder<AlternateTitleEntity> builder)
    {
        builder.ToTable("alternate_title");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.SeriesId).HasColumnName("series_id").HasColumnType("uuid");
        builder.Property(e => e.Title).IsRequired().HasMaxLength(500).HasColumnName("title");
        builder.Property(e => e.Language).HasMaxLength(10).HasColumnName("language");

        builder.HasIndex(e => e.SeriesId).HasDatabaseName("ix_comic_alternate_title_series");
        builder.HasIndex(e => new { e.SeriesId, e.Title, e.Language }).HasDatabaseName("ix_comic_alternate_title_series_title_lang");
    }
}
