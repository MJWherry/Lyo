using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Config.Postgres.Database;

public sealed class ConfigBindingEntityConfiguration : IEntityTypeConfiguration<ConfigBindingEntity>
{
    public void Configure(EntityTypeBuilder<ConfigBindingEntity> builder)
    {
        builder.ToTable("config_binding");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.DefinitionId).IsRequired().HasColumnName("definition_id").HasColumnType("uuid");
        builder.Property(e => e.Key).HasMaxLength(200).IsRequired().HasColumnName("key");
        builder.Property(e => e.ForEntityType).HasMaxLength(500).IsRequired().HasColumnName("for_entity_type");
        builder.Property(e => e.ForEntityId).HasMaxLength(200).IsRequired().HasColumnName("for_entity_id");
        builder.Property(e => e.ValueType).HasMaxLength(2048).IsRequired().HasColumnName("value_type");
        builder.Property(e => e.CreatedTimestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
        builder.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
        builder.HasOne(e => e.Definition).WithMany().HasForeignKey(e => e.DefinitionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => new { e.ForEntityType, e.ForEntityId }).HasDatabaseName("ix_config_binding_entity");
        builder.HasIndex(e => new { e.DefinitionId, e.ForEntityType, e.ForEntityId }).IsUnique().HasDatabaseName("ux_config_binding_definition_entity");
    }
}