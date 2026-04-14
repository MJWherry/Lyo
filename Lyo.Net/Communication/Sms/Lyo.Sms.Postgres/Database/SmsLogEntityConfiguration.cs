using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Sms.Postgres.Database;

public sealed class SmsLogEntityConfiguration : IEntityTypeConfiguration<SmsLogEntity>
{
    public void Configure(EntityTypeBuilder<SmsLogEntity> builder)
    {
        builder.ToTable("sms_logs");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).IsRequired().HasColumnName("id");
        builder.Property(e => e.To).HasMaxLength(50).IsRequired().HasColumnName("to");
        builder.Property(e => e.From).HasMaxLength(50).HasColumnName("from");
        builder.Property(e => e.Body).HasMaxLength(2000).HasColumnName("body");
        builder.Property(e => e.MediaUrlsJson).HasMaxLength(5000).HasColumnName("media_urls_json");
        builder.Property(e => e.IsSuccess).IsRequired().HasColumnName("is_success");
        builder.Property(e => e.Message).HasMaxLength(1000).HasColumnName("message");
        builder.Property(e => e.ErrorMessage).HasMaxLength(2000).HasColumnName("error_message");
        builder.Property(e => e.ElapsedTimeMs).IsRequired().HasColumnName("elapsed_time_ms");
        builder.Property(e => e.MessageId).HasMaxLength(200).HasColumnName("message_id");
        builder.Property(e => e.Status).HasMaxLength(100).HasColumnName("status");
        builder.Property(e => e.ErrorCode).HasColumnName("error_code");
        builder.Property(e => e.DateCreated).HasColumnName("date_created");
        builder.Property(e => e.DateSent).HasColumnName("date_sent");
        builder.Property(e => e.DateUpdated).HasColumnName("date_updated");
        builder.Property(e => e.CreatedAt).IsRequired().HasColumnName("created_at");

        // Indexes for performance
        builder.HasIndex(e => e.To).HasDatabaseName("ix_sms_logs_to");
        builder.HasIndex(e => e.From).HasDatabaseName("ix_sms_logs_from");
        builder.HasIndex(e => e.MessageId).HasDatabaseName("ix_sms_logs_message_id");
        builder.HasIndex(e => e.Status).HasDatabaseName("ix_sms_logs_status");
        builder.HasIndex(e => e.CreatedAt).HasDatabaseName("ix_sms_logs_created_at");
        builder.HasIndex(e => e.IsSuccess).HasDatabaseName("ix_sms_logs_is_success");
        builder.HasIndex(e => new { e.IsSuccess, e.CreatedAt }).HasDatabaseName("ix_sms_logs_is_success_created_at");
        builder.HasIndex(e => new { e.To, e.CreatedAt }).HasDatabaseName("ix_sms_logs_to_created_at");
    }
}