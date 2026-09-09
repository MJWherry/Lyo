using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.FileSystemWatcher.Postgres.Database;

public sealed class FileSystemChangeEntityConfiguration : IEntityTypeConfiguration<FileSystemChangeEntity>
{
    public void Configure(EntityTypeBuilder<FileSystemChangeEntity> builder)
    {
        builder.ToTable("change");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.WatchId).HasColumnName("watch_id").HasColumnType("uuid").IsRequired();
        builder.Property(e => e.SnapshotId).HasColumnName("snapshot_id").HasColumnType("uuid");
        builder.Property(e => e.OccurredAtUtc).HasColumnName("occurred_at_utc").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(e => e.ChangeType).HasColumnName("change_type").HasMaxLength(16).IsRequired();
        builder.Property(e => e.IsDirectory).HasColumnName("is_directory");
        builder.Property(e => e.OldPath).HasColumnName("old_path").HasMaxLength(2048);
        builder.Property(e => e.NewPath).HasColumnName("new_path").HasMaxLength(2048);
        builder.Property(e => e.ChangeJson).HasColumnName("change_json").HasColumnType("jsonb").IsRequired();
        builder.HasOne(e => e.Watch).WithMany(e => e.Changes).HasForeignKey(e => e.WatchId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Snapshot).WithMany(e => e.Changes).HasForeignKey(e => e.SnapshotId).OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(e => new { e.WatchId, e.OccurredAtUtc }).HasDatabaseName("ix_filesystem_watcher_change_watch_occurred");
    }
}
