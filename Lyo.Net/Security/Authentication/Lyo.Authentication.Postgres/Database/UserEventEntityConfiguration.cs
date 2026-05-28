using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Authentication.Postgres.Database;

/// <summary>EF configuration for <see cref="UserEventEntity" />.</summary>
public sealed class UserEventEntityConfiguration : IEntityTypeConfiguration<UserEventEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<UserEventEntity> builder)
    {
        builder.ToTable("event");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid").ValueGeneratedNever();
        builder.Property(e => e.Timestamp).HasColumnName("timestamp").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(e => e.Kind).HasColumnName("kind").HasConversion<string>().HasMaxLength(64).IsRequired();
        builder.Property(e => e.UserId).HasColumnName("user_id").HasColumnType("uuid");
        builder.Property(e => e.TenantId).HasColumnName("tenant_id").HasColumnType("uuid");
        builder.Property(e => e.Subject).HasColumnName("subject").HasMaxLength(255);
        builder.Property(e => e.Provider).HasColumnName("provider").HasMaxLength(100);
        builder.Property(e => e.Outcome).HasColumnName("outcome").HasMaxLength(20);
        builder.Property(e => e.Reason).HasColumnName("reason").HasMaxLength(100);
        builder.Property(e => e.IpAddress).HasColumnName("ip_address").HasMaxLength(64);
        builder.Property(e => e.UserAgent).HasColumnName("user_agent").HasMaxLength(500);
        builder.Property(e => e.CorrelationId).HasColumnName("correlation_id").HasMaxLength(128);
        builder.Property(e => e.MetadataJson).HasColumnName("metadata_json").HasColumnType("jsonb");
        builder.HasIndex(e => e.Timestamp).HasDatabaseName("ix_user_event_timestamp").IsDescending();
        builder.HasIndex(e => e.Kind).HasDatabaseName("ix_user_event_kind");
        builder.HasIndex(e => e.UserId).HasDatabaseName("ix_user_event_user_id").HasFilter("\"user_id\" IS NOT NULL");
        builder.HasIndex(e => e.TenantId).HasDatabaseName("ix_user_event_tenant_id").HasFilter("\"tenant_id\" IS NOT NULL");
        builder.HasOne<UserEntity>().WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.SetNull);
    }
}