using Lyo.EntityReference.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Tag.Postgres.Database;

public sealed class TagEntityConfiguration : EntityRefConfiguration<TagEntity>
{
    public TagEntityConfiguration()
        : base("tag") { }

    /// <inheritdoc />
    public override void Configure(EntityTypeBuilder<TagEntity> builder)
    {
        builder.ToTable("tag");
        builder.HasKey(e => e.Id);
        MapColumns(builder);
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired().HasColumnName("name");
        builder.Property(e => e.TagType).HasMaxLength(50).IsRequired().HasColumnName("tag_type").HasDefaultValue("tag");
        builder.Property(e => e.Slug).HasMaxLength(200).IsRequired().HasColumnName("slug").HasDefaultValue("");
        builder.HasIndex(e => e.Name).HasDatabaseName("ix_tag_name");
        builder.HasIndex(e => e.TagType).HasDatabaseName("ix_tag_tag_type");
        builder.HasIndex(e => new { e.TenantId, e.ForEntityType, e.ForEntityId }).HasDatabaseName("ix_tag_tenant_for_entity");
        builder.HasIndex(e => new { e.TenantId, e.ForEntityType, e.ForEntityId, e.TagType, e.Name, e.Slug })
            .IsUnique()
            .HasDatabaseName("uq_tag_tenant_entity_name_slug_active")
            .HasFilter("\"deleted_at\" IS NULL");
        builder.HasIndex(e => new { e.TenantId, e.Context }).HasDatabaseName("ix_tag_tenant_context");
        builder.HasIndex(e => e.CreatedAt).HasDatabaseName("ix_tag_created_at");
        builder.HasIndex(e => e.ExpiresAt).HasDatabaseName("ix_tag_expires_at").HasFilter("\"expires_at\" IS NOT NULL");
    }
}
