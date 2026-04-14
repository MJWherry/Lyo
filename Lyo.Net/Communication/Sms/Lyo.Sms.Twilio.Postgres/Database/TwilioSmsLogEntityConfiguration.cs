using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Sms.Twilio.Postgres.Database;

/// <summary>Ensures DateTime values are stored as UTC for PostgreSQL timestamp with time zone columns.</summary>
internal static class DateTimeUtcConverter
{
    public static DateTime ToUtc(DateTime value) => value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();

    public static DateTime? ToUtc(DateTime? value) => value == null ? null : ToUtc(value.Value);
}

public sealed class TwilioSmsLogEntityConfiguration : IEntityTypeConfiguration<TwilioSmsLogEntity>
{
    public void Configure(EntityTypeBuilder<TwilioSmsLogEntity> builder)
    {
        builder.ToTable("twilio_sms_logs");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasMaxLength(34).IsRequired().HasColumnName("id");
        builder.Property(e => e.To).HasMaxLength(50).IsRequired().HasColumnName("to");
        builder.Property(e => e.From).HasMaxLength(50).HasColumnName("from");
        builder.Property(e => e.Body).HasMaxLength(2000).HasColumnName("body");
        builder.Property(e => e.MediaUrlsJson).HasMaxLength(5000).HasColumnName("media_urls_json");
        builder.Property(e => e.IsSuccess).IsRequired().HasColumnName("is_success");
        builder.Property(e => e.Message).HasMaxLength(1000).HasColumnName("message");
        builder.Property(e => e.ErrorMessage).HasMaxLength(2000).HasColumnName("error_message");
        builder.Property(e => e.ElapsedTimeMs).IsRequired().HasColumnName("elapsed_time_ms");
        builder.Property(e => e.Status).HasMaxLength(100).HasColumnName("status");
        builder.Property(e => e.ErrorCode).HasColumnName("error_code");
        builder.Property(e => e.DateCreated).HasColumnName("date_created").HasConversion(v => DateTimeUtcConverter.ToUtc(v), v => v);
        builder.Property(e => e.DateSent).HasColumnName("date_sent").HasConversion(v => DateTimeUtcConverter.ToUtc(v), v => v);
        builder.Property(e => e.DateUpdated).HasColumnName("date_updated").HasConversion(v => DateTimeUtcConverter.ToUtc(v), v => v);
        builder.Property(e => e.CreatedTimestamp)
            .IsRequired()
            .HasColumnType("timestamp with time zone")
            .HasColumnName("created_timestamp")
            .HasConversion(v => DateTimeUtcConverter.ToUtc(v), v => v);

        builder.Property(e => e.UpdatedTimestamp)
            .HasColumnType("timestamp with time zone")
            .HasColumnName("updated_timestamp")
            .HasConversion(v => DateTimeUtcConverter.ToUtc(v), v => v);

        builder.Property(e => e.NumSegments).HasColumnName("num_segments");
        builder.Property(e => e.AccountSid).HasMaxLength(100).HasColumnName("account_sid");
        builder.Property(e => e.Price).HasColumnType("numeric(18,4)").HasColumnName("price");
        builder.Property(e => e.PriceUnit).HasMaxLength(10).HasColumnName("price_unit");
        builder.Property(e => e.Direction).IsRequired().HasConversion<string>().HasMaxLength(20).HasColumnName("direction");

        // Indexes for performance
        builder.HasIndex(e => e.To).HasDatabaseName("ix_twilio_sms_logs_to");
        builder.HasIndex(e => e.From).HasDatabaseName("ix_twilio_sms_logs_from");
        builder.HasIndex(e => e.Status).HasDatabaseName("ix_twilio_sms_logs_status");
        builder.HasIndex(e => e.CreatedTimestamp).HasDatabaseName("ix_twilio_sms_logs_created_timestamp");
        builder.HasIndex(e => e.IsSuccess).HasDatabaseName("ix_twilio_sms_logs_is_success");
        builder.HasIndex(e => new { e.IsSuccess, e.CreatedTimestamp }).HasDatabaseName("ix_twilio_sms_logs_is_success_created_timestamp");
        builder.HasIndex(e => new { e.To, e.CreatedTimestamp }).HasDatabaseName("ix_twilio_sms_logs_to_created_timestamp");
        builder.HasIndex(e => e.AccountSid).HasDatabaseName("ix_twilio_sms_logs_account_sid");
        builder.HasIndex(e => e.NumSegments).HasDatabaseName("ix_twilio_sms_logs_num_segments");
    }
}