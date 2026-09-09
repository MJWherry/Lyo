using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Email.Postgres.Database;

/// <summary>Fluent mapping for <see cref="EmailLogEntity" />.</summary>
public sealed class EmailLogEntityConfiguration : IEntityTypeConfiguration<EmailLogEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<EmailLogEntity> builder)
    {
        builder.ToTable("email_logs");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).IsRequired().HasColumnName("id");
        builder.Property(e => e.Direction).IsRequired().HasConversion<string>().HasMaxLength(20).HasColumnName("direction").HasDefaultValue(EmailDirection.Outbound);
        builder.Property(e => e.FromAddress).HasMaxLength(500).HasColumnName("from_address");
        builder.Property(e => e.FromName).HasMaxLength(500).HasColumnName("from_name");
        builder.Property(e => e.ToAddressesJson).HasColumnType("jsonb").HasColumnName("to_addresses_json");
        builder.Property(e => e.CcAddressesJson).HasColumnType("jsonb").HasColumnName("cc_addresses_json");
        builder.Property(e => e.BccAddressesJson).HasColumnType("jsonb").HasColumnName("bcc_addresses_json");
        builder.Property(e => e.Subject).HasMaxLength(1000).HasColumnName("subject");
        builder.Property(e => e.TextBody).HasColumnType("text").HasColumnName("text_body");
        builder.Property(e => e.HtmlFileName).HasMaxLength(500).HasColumnName("html_file_name");
        builder.Property(e => e.HtmlFilePath).HasMaxLength(2000).HasColumnName("html_file_path");
        builder.Property(e => e.IsSuccess).IsRequired().HasColumnName("is_success");
        builder.Property(e => e.Message).HasMaxLength(1000).HasColumnName("message");
        builder.Property(e => e.ErrorMessage).HasMaxLength(2000).HasColumnName("error_message");
        builder.Property(e => e.MessageId).HasMaxLength(200).HasColumnName("message_id");
        builder.Property(e => e.SentTimestamp).HasColumnType("timestamp with time zone").HasColumnName("sent_timestamp");
        builder.Property(e => e.ReceivedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("received_timestamp");
        builder.Property(e => e.CreatedTimestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
        builder.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
        builder.HasIndex(e => e.FromAddress).HasDatabaseName("ix_email_logs_from_address");
        builder.HasIndex(e => e.MessageId).HasDatabaseName("ix_email_logs_message_id");
        builder.HasIndex(e => e.CreatedTimestamp).HasDatabaseName("ix_email_logs_created_timestamp");
        builder.HasIndex(e => e.IsSuccess).HasDatabaseName("ix_email_logs_is_success");
        builder.HasIndex(e => new { e.Direction, e.CreatedTimestamp }).HasDatabaseName("ix_email_logs_direction_created_timestamp");
    }
}
