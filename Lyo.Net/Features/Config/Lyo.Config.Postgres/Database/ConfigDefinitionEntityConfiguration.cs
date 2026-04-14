using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Config.Postgres.Database;

public sealed class ConfigDefinitionEntityConfiguration : IEntityTypeConfiguration<ConfigDefinitionEntity>
{
    public void Configure(EntityTypeBuilder<ConfigDefinitionEntity> builder)
    {
        builder.ToTable("config_definition");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.ForEntityType).HasMaxLength(500).IsRequired().HasColumnName("for_entity_type");
        builder.Property(e => e.Key).HasMaxLength(200).IsRequired().HasColumnName("key");
        builder.Property(e => e.ForValueType).HasMaxLength(2048).IsRequired().HasColumnName("for_value_type");
        builder.Property(e => e.Description).HasMaxLength(4000).HasColumnName("description");
        builder.Property(e => e.IsRequired).HasColumnName("is_required");
        builder.Property(e => e.DefaultValueJson).HasColumnName("default_value_json").HasColumnType("jsonb");
        builder.Property(e => e.CreatedTimestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
        builder.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
        builder.HasIndex(e => new { e.ForEntityType, e.Key }).IsUnique().HasDatabaseName("ux_config_definition_entity_type_key");
        builder.HasIndex(e => e.ForEntityType).HasDatabaseName("ix_config_definition_entity_type");
    }
}