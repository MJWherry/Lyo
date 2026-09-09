using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Drift.Postgres.Database;

public sealed class DriftInstanceConfiguration : IEntityTypeConfiguration<DriftInstance>
{
    public void Configure(EntityTypeBuilder<DriftInstance> builder)
    {
        builder.ToTable("instance");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.InstanceKey).HasColumnName("instance_key").HasMaxLength(256).IsRequired();
        builder.Property(e => e.MachineName).HasColumnName("machine_name").HasMaxLength(200).IsRequired();
        builder.Property(e => e.ProcessId).HasColumnName("process_id");
        builder.Property(e => e.State).HasColumnName("state").HasMaxLength(32).IsRequired();
        builder.Property(e => e.LastHeartbeatUtc).HasColumnName("last_heartbeat_utc").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(e => e.CreatedTimestamp).HasColumnName("created_timestamp").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(e => e.UpdatedTimestamp).HasColumnName("updated_timestamp").HasColumnType("timestamp with time zone");
        builder.Property(e => e.WatchesJson).HasColumnName("watches_json").HasColumnType("jsonb");
        builder.Property(e => e.MetadataJson).HasColumnName("metadata_json").HasColumnType("jsonb");
        builder.HasIndex(e => e.InstanceKey).IsUnique().HasDatabaseName("ux_drift_instance_key");
    }
}
