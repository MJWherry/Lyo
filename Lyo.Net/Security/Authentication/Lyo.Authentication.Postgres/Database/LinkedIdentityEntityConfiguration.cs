using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Authentication.Postgres.Database;

/// <summary>EF configuration for <see cref="LinkedIdentityEntity" />.</summary>
public sealed class LinkedIdentityEntityConfiguration : IEntityTypeConfiguration<LinkedIdentityEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<LinkedIdentityEntity> builder)
    {
        builder.ToTable("linked_identity");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.UserId).HasColumnName("user_id").HasColumnType("uuid").IsRequired();
        builder.Property(e => e.TenantId).HasColumnName("tenant_id").HasColumnType("uuid");
        builder.Property(e => e.Provider).HasColumnName("provider").HasMaxLength(100).IsRequired();
        builder.Property(e => e.Subject).HasColumnName("subject").HasMaxLength(255).IsRequired();
        builder.Property(e => e.EmailAtLink).HasColumnName("email_at_link").HasMaxLength(320);
        builder.Property(e => e.ScopesJson).HasColumnName("scopes_json").HasColumnType("jsonb").IsRequired();
        builder.Property(e => e.RawClaimsJson).HasColumnName("raw_claims_json").HasColumnType("jsonb");
        builder.Property(e => e.LinkedTimestamp).HasColumnName("linked_timestamp").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(e => e.UpdatedTimestamp).HasColumnName("updated_timestamp").HasColumnType("timestamp with time zone");
        builder.Property(e => e.LastUsedTimestamp).HasColumnName("last_used_timestamp").HasColumnType("timestamp with time zone");
        builder.Property(e => e.UnlinkedTimestamp).HasColumnName("unlinked_timestamp").HasColumnType("timestamp with time zone");
        builder.HasOne<UserEntity>().WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => new { e.TenantId, e.Provider, e.Subject })
            .IsUnique()
            .HasDatabaseName("ux_linked_identity_tenant_provider_subject")
            .HasFilter("\"unlinked_timestamp\" IS NULL");

        builder.HasIndex(e => e.UserId).HasDatabaseName("ix_linked_identity_user_id");
        builder.HasIndex(e => e.Provider).HasDatabaseName("ix_linked_identity_provider");
        builder.HasIndex(e => e.TenantId).HasDatabaseName("ix_linked_identity_tenant_id").HasFilter("\"tenant_id\" IS NOT NULL");
    }
}