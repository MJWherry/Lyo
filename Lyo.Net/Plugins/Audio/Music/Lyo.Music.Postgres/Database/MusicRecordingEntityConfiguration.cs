using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Music.Postgres.Database;

public sealed class MusicRecordingEntityConfiguration : IEntityTypeConfiguration<MusicRecordingEntity>
{
    public void Configure(EntityTypeBuilder<MusicRecordingEntity> builder)
    {
        builder.ToTable("recording");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.Title).IsRequired().HasMaxLength(500).HasColumnName("title");
    }
}