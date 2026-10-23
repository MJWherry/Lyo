using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Music.Postgres.Database;

public sealed class MusicIdentifierEntityConfiguration : IEntityTypeConfiguration<MusicIdentifierEntity>
{
    public void Configure(EntityTypeBuilder<MusicIdentifierEntity> builder)
    {
        builder.ToTable("identifier");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.ReleaseId).HasColumnName("release_id").HasColumnType("uuid");
        builder.Property(e => e.Scheme).IsRequired().HasMaxLength(50).HasColumnName("scheme");
        builder.Property(e => e.Value).IsRequired().HasMaxLength(256).HasColumnName("value");
        builder.Property(e => e.NormalizedValue).IsRequired().HasMaxLength(256).HasColumnName("normalized_value");
        builder.Property(e => e.IsPrimary).HasColumnName("is_primary");
        builder.HasIndex(e => new { e.Scheme, e.NormalizedValue }).IsUnique().HasDatabaseName("ix_music_identifier_scheme_normalized");
        builder.HasIndex(e => e.ReleaseId).IsUnique().HasFilter("is_primary").HasDatabaseName("ix_music_identifier_primary");
    }
}