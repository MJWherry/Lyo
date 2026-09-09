using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.FileSystemWatcher.Postgres.Database;

public sealed class FileSystemSnapshotEntityConfiguration : IEntityTypeConfiguration<FileSystemSnapshotEntity>
{
    public void Configure(EntityTypeBuilder<FileSystemSnapshotEntity> builder)
    {
        builder.ToTable("snapshot");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.WatchId).HasColumnName("watch_id").HasColumnType("uuid").IsRequired();
        builder.Property(e => e.TakenAtUtc).HasColumnName("taken_at_utc").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(e => e.FileCount).HasColumnName("file_count");
        builder.Property(e => e.DirectoryCount).HasColumnName("directory_count");
        builder.Property(e => e.ContentHash).HasColumnName("content_hash").HasMaxLength(128).IsRequired();
        builder.Property(e => e.ContentHashAlgorithm).HasColumnName("content_hash_algorithm").HasMaxLength(32).IsRequired();
        builder.Property(e => e.TreeJson).HasColumnName("tree_json").HasColumnType("jsonb").IsRequired();
        builder.HasOne(e => e.Watch).WithMany(e => e.Snapshots).HasForeignKey(e => e.WatchId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => new { e.WatchId, e.TakenAtUtc }).HasDatabaseName("ix_filesystem_watcher_snapshot_watch_taken");
        builder.HasIndex(e => new { e.WatchId, e.ContentHashAlgorithm, e.ContentHash }).HasDatabaseName("ix_filesystem_watcher_snapshot_watch_hash");
    }
}
