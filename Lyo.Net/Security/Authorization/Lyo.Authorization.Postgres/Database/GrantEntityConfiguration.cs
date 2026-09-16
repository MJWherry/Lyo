using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Authorization.Postgres.Database;

/// <summary>Maps <see cref="GrantEntity" /> and the active-grant unique index.</summary>
public sealed class GrantEntityConfiguration : IEntityTypeConfiguration<GrantEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<GrantEntity> builder)
    {
        builder.ToTable("grant");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.Subject).HasColumnName("subject").HasMaxLength(512).IsRequired();
        builder.Property(e => e.Action).HasColumnName("action").HasMaxLength(128).IsRequired();
        builder.Property(e => e.ResourceType).HasColumnName("resource_type").HasMaxLength(128).IsRequired();
        builder.Property(e => e.ResourceId).HasColumnName("resource_id").HasMaxLength(256);
        builder.Property(e => e.TenantId).HasColumnName("tenant_id");
        builder.Property(e => e.GrantedBy).HasColumnName("granted_by").HasMaxLength(512);
        builder.Property(e => e.ExpiresAt).HasColumnName("expires_at").HasColumnType("timestamp with time zone");
        builder.Property(e => e.RevokedAt).HasColumnName("revoked_at").HasColumnType("timestamp with time zone");
        builder.Property(e => e.CreatedTimestamp).HasColumnName("created_timestamp").HasColumnType("timestamp with time zone");
        builder.HasIndex(e => new { e.Subject, e.Action, e.ResourceType, e.ResourceId, e.TenantId })
            .IsUnique()
            .HasFilter("revoked_at IS NULL")
            .AreNullsDistinct(false)
            .HasDatabaseName("ix_grant_active_key");
    }
}
