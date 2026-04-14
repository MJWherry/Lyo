using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.FileMetadataStore.Postgres.Database;

public sealed class FileAuditEventEntityConfiguration : IEntityTypeConfiguration<FileAuditEventEntity>
{
    public void Configure(EntityTypeBuilder<FileAuditEventEntity> builder)
    {
        builder.ToTable("file_audit_events");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).IsRequired().HasColumnType("uuid").HasColumnName("id");
        builder.Property(e => e.EventType).IsRequired().HasMaxLength(64).HasColumnName("event_type");
        builder.Property(e => e.Timestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("timestamp");
        builder.Property(e => e.FileId).HasColumnType("uuid").HasColumnName("file_id");
        builder.Property(e => e.TenantId).HasMaxLength(256).HasColumnName("tenant_id");
        builder.Property(e => e.ActorId).HasMaxLength(256).HasColumnName("actor_id");
        builder.Property(e => e.DataEncryptionKeyId).HasMaxLength(255).HasColumnName("data_encryption_key_id");
        builder.Property(e => e.DataEncryptionKeyVersion).HasMaxLength(255).HasColumnName("data_encryption_key_version");
        builder.Property(e => e.Outcome).IsRequired().HasMaxLength(32).HasColumnName("outcome");
        builder.Property(e => e.Error).HasMaxLength(2000).HasColumnName("error");
        builder.Property(e => e.CorrelationId).HasColumnType("uuid").HasColumnName("correlation_id");
        builder.HasIndex(e => e.Timestamp).HasDatabaseName("ix_file_audit_events_timestamp");
        builder.HasIndex(e => new { e.TenantId, e.Timestamp }).HasDatabaseName("ix_file_audit_events_tenant_timestamp");
        builder.HasIndex(e => e.FileId).HasDatabaseName("ix_file_audit_events_file_id");
        builder.HasIndex(e => e.EventType).HasDatabaseName("ix_file_audit_events_event_type");
    }
}