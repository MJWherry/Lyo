using Lyo.EntityReference.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Audit.Postgres.Database;

public sealed class AuditEventEntityConfiguration : IEntityTypeConfiguration<AuditEventEntity>
{
    public void Configure(EntityTypeBuilder<AuditEventEntity> builder)
    {
        builder.ToTable("audit_events");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.EventType).HasMaxLength(200).IsRequired().HasColumnName("event_type");
        builder.Property(e => e.Timestamp).IsRequired().HasColumnName("timestamp").HasColumnType("timestamp with time zone");
        builder.MapOptionalFromStringAssociationColumns();
        builder.Property(e => e.Message).HasMaxLength(4000).HasColumnName("message");
        builder.Property(e => e.MetadataJson).HasColumnName("metadata_json").HasColumnType("jsonb").HasMaxLength(8192);
        builder.Property(e => e.CreatedTimestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
        builder.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
        builder.HasIndex(e => e.Timestamp).HasDatabaseName("ix_audit_events_timestamp");
        builder.HasIndex(e => e.EventType).HasDatabaseName("ix_audit_events_event_type");
        builder.HasIndex(e => new { e.EventType, e.Timestamp }).HasDatabaseName("ix_audit_events_event_type_timestamp");
        builder.HasIndex(e => new { e.ForEntityType, e.ForEntityId, e.Timestamp }).HasDatabaseName("ix_audit_events_for_entity_timestamp");
        builder.HasIndex(e => new { e.FromEntityType, e.FromEntityId }).HasDatabaseName("ix_audit_events_from_entity");
        builder.HasIndex(e => e.TenantId).HasDatabaseName("ix_audit_events_tenant");
    }
}