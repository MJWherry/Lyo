using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Audit.Postgres.Database;

public sealed class AuditChangeEntityConfiguration : IEntityTypeConfiguration<AuditChangeEntity>
{
    public void Configure(EntityTypeBuilder<AuditChangeEntity> builder)
    {
        builder.ToTable("audit_changes");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.Timestamp).IsRequired().HasColumnName("timestamp");
        builder.Property(e => e.CreatedTimestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
        builder.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
        builder.Property(e => e.TypeAssemblyFullName).HasMaxLength(500).IsRequired().HasColumnName("type_assembly_full_name");
        builder.Property(e => e.OldValuesJson).IsRequired().HasColumnName("old_values_json").HasColumnType("jsonb");
        builder.Property(e => e.ChangedPropertiesJson).IsRequired().HasColumnName("changed_properties_json").HasColumnType("jsonb");
        builder.HasIndex(e => e.Timestamp).HasDatabaseName("ix_audit_changes_timestamp");
        builder.HasIndex(e => e.TypeAssemblyFullName).HasDatabaseName("ix_audit_changes_type");
    }
}