using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Config.Postgres.Database;

public sealed class ConfigDefinitionRevisionEntityConfiguration : IEntityTypeConfiguration<ConfigDefinitionRevisionEntity>
{
    public void Configure(EntityTypeBuilder<ConfigDefinitionRevisionEntity> builder)
    {
        builder.ToTable("config_definition_revision");
        builder.HasKey(e => new { e.DefinitionId, e.Revision });
        builder.Property(e => e.DefinitionId).IsRequired().HasColumnName("definition_id").HasColumnType("uuid");
        builder.Property(e => e.Revision).IsRequired().HasColumnName("revision");
        builder.Property(e => e.Key).HasMaxLength(200).IsRequired().HasColumnName("key");
        builder.Property(e => e.ForValueType).HasMaxLength(1024).IsRequired().HasColumnName("for_value_type");
        builder.Property(e => e.Description).HasMaxLength(4000).HasColumnName("description");
        builder.Property(e => e.IsRequired).HasColumnName("is_required");
        builder.Property(e => e.IsEncrypted).HasColumnName("is_encrypted");
        builder.Property(e => e.DefaultValueJson).HasColumnName("default_value_json").HasColumnType("jsonb").HasMaxLength(8192);
        builder.Property(e => e.EncryptedDefaultValue).HasColumnName("encrypted_default_value").HasColumnType("bytea");
        builder.Property(e => e.CreatedTimestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
        builder.HasOne(e => e.Definition).WithMany().HasForeignKey(e => e.DefinitionId).OnDelete(DeleteBehavior.Cascade);
    }
}
