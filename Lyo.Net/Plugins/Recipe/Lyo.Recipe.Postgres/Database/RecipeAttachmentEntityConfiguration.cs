using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Recipe.Postgres.Database;

public sealed class RecipeAttachmentEntityConfiguration : IEntityTypeConfiguration<RecipeAttachmentEntity>
{
    public void Configure(EntityTypeBuilder<RecipeAttachmentEntity> builder)
    {
        builder.ToTable("attachment");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.TenantId).HasColumnType("uuid").HasColumnName("tenant_id");
        builder.Property(e => e.EntityType).HasMaxLength(80).IsRequired().HasColumnName("entity_type");
        builder.Property(e => e.EntityId).HasColumnName("entity_id").HasColumnType("uuid");
        builder.Property(e => e.FileId).HasColumnName("file_id").HasColumnType("uuid");
        builder.Property(e => e.Role).HasColumnName("role");
        builder.Property(e => e.Caption).HasMaxLength(500).HasColumnName("caption");
        builder.Property(e => e.IsPrimary).HasColumnName("is_primary");
        builder.Property(e => e.SortOrder).HasColumnName("sort_order");
        builder.Property(e => e.CreatedTimestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
        builder.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
        builder.HasIndex(e => new { e.EntityType, e.EntityId }).HasDatabaseName("ix_recipe_attachment_subject");
        builder.HasIndex(e => e.FileId).HasDatabaseName("ix_recipe_attachment_file");
        builder.HasIndex(e => e.TenantId).HasDatabaseName("ix_recipe_attachment_tenant").HasFilter("\"tenant_id\" IS NOT NULL");
    }
}