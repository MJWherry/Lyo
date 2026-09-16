using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Idempotency.Postgres.Database;

/// <summary>Maps <see cref="IdempotencyEntryEntity" /> to <c>idempotency.entry</c>.</summary>
public sealed class IdempotencyEntryEntityConfiguration : IEntityTypeConfiguration<IdempotencyEntryEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<IdempotencyEntryEntity> builder)
    {
        builder.ToTable("entry");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.Kind).HasColumnName("kind").HasMaxLength(16).IsRequired();
        builder.Property(e => e.Scope).HasColumnName("scope").HasMaxLength(1024).IsRequired();
        builder.Property(e => e.Key).HasColumnName("key").HasMaxLength(255).IsRequired();
        builder.Property(e => e.RequestHash).HasColumnName("request_hash").IsRequired();
        builder.Property(e => e.State).HasColumnName("state").HasMaxLength(16).IsRequired();
        builder.Property(e => e.LeaseToken).HasColumnName("lease_token");
        builder.Property(e => e.LeaseUntil).HasColumnName("lease_until").HasColumnType("timestamp with time zone");
        builder.Property(e => e.ResponseStatus).HasColumnName("response_status");
        builder.Property(e => e.ResponseBody).HasColumnName("response_body");
        builder.Property(e => e.ResponseContentType).HasColumnName("response_content_type").HasMaxLength(128);
        builder.Property(e => e.ResponseOmitted).HasColumnName("response_omitted");
        builder.Property(e => e.Destination).HasColumnName("destination").HasMaxLength(256);
        builder.Property(e => e.Payload).HasColumnName("payload");
        builder.Property(e => e.AttemptCount).HasColumnName("attempt_count");
        builder.Property(e => e.AvailableAt).HasColumnName("available_at").HasColumnType("timestamp with time zone");
        builder.Property(e => e.ExpiresAt).HasColumnName("expires_at").HasColumnType("timestamp with time zone");
        builder.Property(e => e.CreatedTimestamp).HasColumnName("created_timestamp").HasColumnType("timestamp with time zone");
        builder.Property(e => e.CompletedTimestamp).HasColumnName("completed_timestamp").HasColumnType("timestamp with time zone");
        builder.HasIndex(e => new { e.Scope, e.Key })
            .HasFilter("state IN ('in_progress', 'completed', 'failed')")
            .IsUnique()
            .HasDatabaseName("ix_idempotency_entry_live_scope_key");
        builder.HasIndex(e => new { e.Kind, e.State, e.AvailableAt })
            .HasFilter("kind = 'outbox' AND state = 'in_progress'")
            .HasDatabaseName("ix_idempotency_entry_outbox_due");
    }
}
