using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Comic.Postgres.Database;

public sealed class CharacterEntityConfiguration : IEntityTypeConfiguration<CharacterEntity>
{
    public void Configure(EntityTypeBuilder<CharacterEntity> builder)
    {
        builder.ToTable("character");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.SeriesId).HasColumnName("series_id").HasColumnType("uuid");
        builder.Property(e => e.Name).IsRequired().HasMaxLength(500).HasColumnName("name");
        builder.Property(e => e.Description).HasColumnName("description");
        builder.Property(e => e.ImageRef).HasColumnName("image_ref");
        builder.Property(e => e.Role).HasMaxLength(50).HasColumnName("role");
        builder.Property(e => e.CreatedTimestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
        builder.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");

        builder.HasOne(e => e.Series)
               .WithMany(s => s.Characters)
               .HasForeignKey(e => e.SeriesId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Volumes)
               .WithMany(v => v.Characters)
               .UsingEntity(j => j.ToTable("character_volume"));

        builder.HasIndex(e => e.SeriesId).HasDatabaseName("ix_comic_character_series");
        builder.HasIndex(e => e.Name).HasDatabaseName("ix_comic_character_name");
    }
}
