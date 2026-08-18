using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Validation.Postgres.Database;

/// <summary>Fluent mapping for <see cref="ValidationSchemaEntity" />.</summary>
public sealed class ValidationSchemaEntityConfiguration : IEntityTypeConfiguration<ValidationSchemaEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ValidationSchemaEntity> builder)
    {
        builder.ToTable("schema");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.Key).HasMaxLength(200).IsRequired().HasColumnName("key");
        builder.Property(e => e.TargetTypeName).HasMaxLength(512).HasColumnName("target_type_name");
        builder.Property(e => e.Description).HasMaxLength(4000).HasColumnName("description");
        builder.Property(e => e.ConstraintsJson).HasColumnName("constraints_json").HasColumnType("jsonb").IsRequired();
        builder.Property(e => e.MessagesJson).HasColumnName("messages_json").HasColumnType("jsonb");
        builder.Property(e => e.CreatedTimestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
        builder.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
        builder.HasIndex(e => e.Key).IsUnique().HasDatabaseName("ux_validation_schema_key");
        builder.HasIndex(e => e.TargetTypeName).HasDatabaseName("ix_validation_schema_target_type");
    }
}
