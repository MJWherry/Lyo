using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Drift.Postgres.Database;

public sealed class DriftStructureSnapshotConfiguration : IEntityTypeConfiguration<DriftStructureSnapshot>
{
    public void Configure(EntityTypeBuilder<DriftStructureSnapshot> builder)
    {
        builder.ToTable("structure_snapshot");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.InstanceId).HasColumnName("instance_id").HasColumnType("uuid").IsRequired();
        builder.Property(e => e.Kind).HasColumnName("kind").HasMaxLength(32).IsRequired();
        builder.Property(e => e.WatchRoot).HasColumnName("watch_root").HasMaxLength(2048).IsRequired();
        builder.Property(e => e.TakenAtUtc).HasColumnName("taken_at_utc").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(e => e.ReceivedAtUtc).HasColumnName("received_at_utc").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(e => e.ContentHash).HasColumnName("content_hash").HasMaxLength(128).IsRequired();
        builder.Property(e => e.ContentHashAlgorithm).HasColumnName("content_hash_algorithm").HasMaxLength(32).IsRequired();
        builder.Property(e => e.TreeJson).HasColumnName("tree_json").HasColumnType("jsonb");
        builder.Property(e => e.SystemInfoJson).HasColumnName("system_info_json").HasColumnType("jsonb");
        builder.HasOne(e => e.Instance).WithMany(e => e.Snapshots).HasForeignKey(e => e.InstanceId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => new { e.InstanceId, e.Kind, e.WatchRoot, e.TakenAtUtc }).HasDatabaseName("ix_drift_snapshot_lineage_taken");
        builder.HasIndex(e => new { e.InstanceId, e.Kind, e.WatchRoot, e.ContentHashAlgorithm, e.ContentHash }).HasDatabaseName("ix_drift_snapshot_lineage_hash");
    }
}
