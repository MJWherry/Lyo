using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Authentication.Postgres.Database;

/// <summary>EF configuration for <see cref="UserEntity"/>.</summary>
public sealed class UserEntityConfiguration : IEntityTypeConfiguration<UserEntity>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<UserEntity> builder)
    {
        builder.ToTable("user");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.DisplayName).HasColumnName("display_name").HasMaxLength(200).IsRequired();
        builder.Property(e => e.Email).HasColumnName("email").HasColumnType("citext").HasMaxLength(320).IsRequired();
        builder.Property(e => e.EmailVerified).HasColumnName("email_verified").IsRequired().HasDefaultValue(false);
        builder.Property(e => e.AvatarUrl).HasColumnName("avatar_url").HasMaxLength(2048);
        builder.Property(e => e.PreferredLanguageBcp47).HasColumnName("preferred_language_bcp47").HasMaxLength(20);
        builder.Property(e => e.ScopesJson).HasColumnName("scopes_json").HasColumnType("jsonb").IsRequired();
        builder.Property(e => e.MetadataJson).HasColumnName("metadata_json").HasColumnType("jsonb");
        builder.Property(e => e.PersonId).HasColumnName("person_id").HasColumnType("uuid");
        builder.Property(e => e.TenantId).HasColumnName("tenant_id").HasColumnType("uuid");
        builder.Property(e => e.CreatedTimestamp).HasColumnName("created_timestamp").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(e => e.UpdatedTimestamp).HasColumnName("updated_timestamp").HasColumnType("timestamp with time zone");
        builder.Property(e => e.LastLoginTimestamp).HasColumnName("last_login_timestamp").HasColumnType("timestamp with time zone");
        builder.Property(e => e.DisabledTimestamp).HasColumnName("disabled_timestamp").HasColumnType("timestamp with time zone");
        builder.Property(e => e.DisabledReason).HasColumnName("disabled_reason").HasMaxLength(500);
        builder.HasIndex(e => new { e.TenantId, e.Email }).IsUnique().HasDatabaseName("ux_user_tenant_email");
        builder.HasIndex(e => e.DisabledTimestamp)
            .HasDatabaseName("ix_user_disabled_timestamp")
            .HasFilter("\"disabled_timestamp\" IS NULL");
        builder.HasIndex(e => e.LastLoginTimestamp).HasDatabaseName("ix_user_last_login_timestamp").IsDescending();
        builder.HasIndex(e => e.PersonId)
            .HasDatabaseName("ix_user_person_id")
            .HasFilter("\"person_id\" IS NOT NULL");
        builder.HasIndex(e => e.TenantId)
            .HasDatabaseName("ix_user_tenant_id")
            .HasFilter("\"tenant_id\" IS NOT NULL");
    }
}
