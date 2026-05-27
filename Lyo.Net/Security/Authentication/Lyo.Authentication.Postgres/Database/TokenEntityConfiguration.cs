using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Authentication.Postgres.Database;

/// <summary>EF configuration for <see cref="TokenEntity"/>.</summary>
public sealed class TokenEntityConfiguration : IEntityTypeConfiguration<TokenEntity>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<TokenEntity> builder)
    {
        builder.ToTable("token");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasMaxLength(11).IsRequired();
        builder.Property(e => e.SecretHash).HasColumnName("secret_hash").HasColumnType("bytea").IsRequired();
        builder.Property(e => e.Kind).HasColumnName("kind").HasMaxLength(20).IsRequired();
        builder.Property(e => e.Ring).HasColumnName("ring").HasMaxLength(10).IsRequired();
        builder.Property(e => e.UserId).HasColumnName("user_id").HasColumnType("uuid");
        builder.Property(e => e.TenantId).HasColumnName("tenant_id").HasColumnType("uuid");
        builder.Property(e => e.DisplayName).HasColumnName("display_name").HasMaxLength(200).IsRequired();
        builder.Property(e => e.ScopesJson).HasColumnName("scopes_json").HasColumnType("jsonb").IsRequired();
        builder.Property(e => e.MetadataJson).HasColumnName("metadata_json").HasColumnType("jsonb");
        builder.Property(e => e.CreatedTimestamp).HasColumnName("created_timestamp").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(e => e.UpdatedTimestamp).HasColumnName("updated_timestamp").HasColumnType("timestamp with time zone");
        builder.Property(e => e.ExpiresTimestamp).HasColumnName("expires_timestamp").HasColumnType("timestamp with time zone");
        builder.Property(e => e.LastUsedTimestamp).HasColumnName("last_used_timestamp").HasColumnType("timestamp with time zone");
        builder.Property(e => e.RevokedTimestamp).HasColumnName("revoked_timestamp").HasColumnType("timestamp with time zone");
        builder.Property(e => e.RevokedReason).HasColumnName("revoked_reason").HasMaxLength(500);
        builder.Property(e => e.RotatedFromId).HasColumnName("rotated_from_id").HasMaxLength(11);
        builder.HasOne<UserEntity>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<TokenEntity>()
            .WithMany()
            .HasForeignKey(e => e.RotatedFromId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(e => new { e.UserId, e.RevokedTimestamp }).HasDatabaseName("ix_token_user_id_revoked_timestamp");
        builder.HasIndex(e => e.ExpiresTimestamp)
            .HasDatabaseName("ix_token_expires_timestamp")
            .HasFilter("\"expires_timestamp\" IS NOT NULL");
        builder.HasIndex(e => new { e.Kind, e.Ring }).HasDatabaseName("ix_token_kind_ring");
        builder.HasIndex(e => e.TenantId)
            .HasDatabaseName("ix_token_tenant_id")
            .HasFilter("\"tenant_id\" IS NOT NULL");
    }
}
