using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Drift.Postgres.Database;

public sealed class DriftChangeEventConfiguration : IEntityTypeConfiguration<DriftChangeEvent>
{
    public void Configure(EntityTypeBuilder<DriftChangeEvent> builder)
    {
        builder.ToTable("change_event");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.InstanceId).HasColumnName("instance_id").HasColumnType("uuid").IsRequired();
        builder.Property(e => e.WatchRoot).HasColumnName("watch_root").HasMaxLength(2048).IsRequired();
        builder.Property(e => e.SnapshotId).HasColumnName("snapshot_id").HasColumnType("uuid");
        builder.Property(e => e.OccurredAtUtc).HasColumnName("occurred_at_utc").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(e => e.ChangeJson).HasColumnName("change_json").HasColumnType("jsonb").IsRequired();
        builder.HasOne(e => e.Instance).WithMany(e => e.Changes).HasForeignKey(e => e.InstanceId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => new { e.InstanceId, e.WatchRoot, e.OccurredAtUtc }).HasDatabaseName("ix_drift_change_instance_root_occurred");
    }
}
