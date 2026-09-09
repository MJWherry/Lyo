using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.FileSystemWatcher.Postgres.Database;

public sealed class FileSystemWatchEntityConfiguration : IEntityTypeConfiguration<FileSystemWatchEntity>
{
    public void Configure(EntityTypeBuilder<FileSystemWatchEntity> builder)
    {
        builder.ToTable("watch");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.RootPath).HasColumnName("root_path").HasMaxLength(2048).IsRequired();
        builder.Property(e => e.OptionsJson).HasColumnName("options_json").HasColumnType("jsonb").IsRequired();
        builder.Property(e => e.CreatedTimestamp).HasColumnName("created_timestamp").HasColumnType("timestamp with time zone").IsRequired();
        builder.HasIndex(e => e.RootPath).HasDatabaseName("ix_filesystem_watcher_watch_root");
    }
}
