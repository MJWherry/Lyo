using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Authentication.Postgres.Database;

/// <summary>EF mapping for <see cref="UserScopeEntity" />.</summary>
public sealed class UserScopeEntityConfiguration : IEntityTypeConfiguration<UserScopeEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<UserScopeEntity> builder)
    {
        builder.ToTable("scope");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(e => e.UserId).HasColumnName("user_id").HasColumnType("uuid").IsRequired();
        builder.Property(e => e.TenantId).HasColumnName("tenant_id").HasColumnType("uuid");
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(e => e.CreatedTimestamp).HasColumnName("created_timestamp").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(e => e.UpdatedTimestamp).HasColumnName("updated_timestamp").HasColumnType("timestamp with time zone");
        builder.HasOne<UserEntity>().WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => e.UserId).HasDatabaseName("ix_scope_user_id");
        builder.HasIndex(e => e.TenantId).HasDatabaseName("ix_scope_tenant_id").HasFilter("\"tenant_id\" IS NOT NULL");
        builder.HasIndex(e => new { e.UserId, e.Name }).IsUnique().HasDatabaseName("ux_scope_user_id_name");
    }
}
