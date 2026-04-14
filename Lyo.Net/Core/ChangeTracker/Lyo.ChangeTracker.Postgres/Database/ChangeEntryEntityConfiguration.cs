using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.ChangeTracker.Postgres.Database;

public sealed class ChangeEntryEntityConfiguration : IEntityTypeConfiguration<ChangeEntryEntity>
{
    public void Configure(EntityTypeBuilder<ChangeEntryEntity> builder)
    {
        builder.ToTable("changes");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.Timestamp).IsRequired().HasColumnName("timestamp").HasColumnType("timestamp with time zone");
        builder.Property(e => e.ForEntityType).HasMaxLength(500).IsRequired().HasColumnName("for_entity_type");
        builder.Property(e => e.ForEntityId).HasMaxLength(500).IsRequired().HasColumnName("for_entity_id");
        builder.Property(e => e.FromEntityType).HasMaxLength(500).HasColumnName("from_entity_type");
        builder.Property(e => e.FromEntityId).HasMaxLength(500).HasColumnName("from_entity_id");
        builder.Property(e => e.ChangeType).HasMaxLength(200).HasColumnName("change_type");
        builder.Property(e => e.Message).HasMaxLength(4000).HasColumnName("message");
        builder.Property(e => e.OldValuesJson).IsRequired().HasColumnName("old_values_json").HasColumnType("jsonb");
        builder.Property(e => e.ChangedPropertiesJson).IsRequired().HasColumnName("changed_properties_json").HasColumnType("jsonb");
        builder.Property(e => e.CreatedTimestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
        builder.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
        builder.HasIndex(e => e.Timestamp).HasDatabaseName("ix_changes_timestamp");
        builder.HasIndex(e => new { e.ForEntityType, e.ForEntityId, e.Timestamp }).HasDatabaseName("ix_changes_for_entity_timestamp");
        builder.HasIndex(e => e.ForEntityType).HasDatabaseName("ix_changes_for_entity_type");
        builder.HasIndex(e => new { e.FromEntityType, e.FromEntityId }).HasDatabaseName("ix_changes_from_entity");
    }
}