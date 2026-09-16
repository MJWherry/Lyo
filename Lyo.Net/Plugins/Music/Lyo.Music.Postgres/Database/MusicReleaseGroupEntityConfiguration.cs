using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Music.Postgres.Database;

public sealed class MusicReleaseGroupEntityConfiguration : IEntityTypeConfiguration<MusicReleaseGroupEntity>
{
    public void Configure(EntityTypeBuilder<MusicReleaseGroupEntity> builder)
    {
        builder.ToTable("release_group");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.Title).IsRequired().HasMaxLength(500).HasColumnName("title");
        builder.Property(e => e.Slug).IsRequired().HasMaxLength(500).HasColumnName("slug");
        builder.Property(e => e.Kind).HasConversion<string>().HasColumnType("text").HasColumnName("kind");
        builder.Property(e => e.Status).HasConversion<string>().HasColumnType("text").HasColumnName("status");
        builder.Property(e => e.CreatedTimestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
        builder.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
        builder.HasIndex(e => e.Slug).IsUnique().HasDatabaseName("ix_music_release_group_slug");
        builder.HasMany(e => e.Releases).WithOne(r => r.ReleaseGroup).HasForeignKey(r => r.ReleaseGroupId).OnDelete(DeleteBehavior.SetNull);
    }
}
