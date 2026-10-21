using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Music.Postgres.Database;

public sealed class MusicCreditEntityConfiguration : IEntityTypeConfiguration<MusicCreditEntity>
{
    public void Configure(EntityTypeBuilder<MusicCreditEntity> builder)
    {
        builder.ToTable("credit");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.ReleaseId).HasColumnName("release_id").HasColumnType("uuid");
        builder.Property(e => e.TrackId).HasColumnName("track_id").HasColumnType("uuid");
        builder.Property(e => e.DisplayName).IsRequired().HasMaxLength(500).HasColumnName("display_name");
        builder.Property(e => e.SortName).HasMaxLength(500).HasColumnName("sort_name");
        builder.Property(e => e.Role).HasConversion<string>().HasColumnType("text").HasColumnName("role");
        builder.Property(e => e.Ordinal).HasColumnName("ordinal");
        builder.Property(e => e.PersonRef).HasMaxLength(256).HasColumnName("person_ref");
        builder.HasIndex(e => new { e.ReleaseId, e.Role, e.Ordinal }).IsUnique().HasFilter("release_id IS NOT NULL").HasDatabaseName("ix_music_credit_release_role_ordinal");
        builder.HasIndex(e => new { e.TrackId, e.Role, e.Ordinal }).IsUnique().HasFilter("track_id IS NOT NULL").HasDatabaseName("ix_music_credit_track_role_ordinal");
    }
}