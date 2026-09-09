using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Drift.Postgres.Database;

public sealed class DriftDiffSnapshotConfiguration : IEntityTypeConfiguration<DriftDiffSnapshot>
{
    public void Configure(EntityTypeBuilder<DriftDiffSnapshot> builder)
    {
        builder.ToTable("diff_snapshot");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.InstanceId).HasColumnName("instance_id").HasColumnType("uuid").IsRequired();
        builder.Property(e => e.FromSnapshotId).HasColumnName("from_snapshot_id").HasColumnType("uuid");
        builder.Property(e => e.ToSnapshotId).HasColumnName("to_snapshot_id").HasColumnType("uuid").IsRequired();
        builder.Property(e => e.Source).HasColumnName("source").HasMaxLength(32).IsRequired();
        builder.Property(e => e.ComputedAtUtc).HasColumnName("computed_at_utc").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(e => e.FileChangesJson).HasColumnName("file_changes_json").HasColumnType("jsonb");
        builder.Property(e => e.SystemDifferencesJson).HasColumnName("system_differences_json").HasColumnType("jsonb");
        builder.HasOne(e => e.Instance).WithMany(e => e.Diffs).HasForeignKey(e => e.InstanceId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => new { e.InstanceId, e.ComputedAtUtc }).HasDatabaseName("ix_drift_diff_instance_computed");
        builder.HasIndex(e => e.ToSnapshotId).HasDatabaseName("ix_drift_diff_to_snapshot");
    }
}
