using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Email.Postgres.Database;

public sealed class EmailLogEntityConfiguration : IEntityTypeConfiguration<EmailLogEntity>
{
    public void Configure(EntityTypeBuilder<EmailLogEntity> builder)
    {
        builder.ToTable("email_logs");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).IsRequired().HasColumnName("id");
        builder.Property(e => e.FromAddress).HasMaxLength(500).HasColumnName("from_address");
        builder.Property(e => e.FromName).HasMaxLength(500).HasColumnName("from_name");
        builder.Property(e => e.ToAddressesJson).HasMaxLength(4000).HasColumnName("to_addresses_json");
        builder.Property(e => e.CcAddressesJson).HasMaxLength(4000).HasColumnName("cc_addresses_json");
        builder.Property(e => e.BccAddressesJson).HasMaxLength(4000).HasColumnName("bcc_addresses_json");
        builder.Property(e => e.Subject).HasMaxLength(1000).HasColumnName("subject");
        builder.Property(e => e.IsSuccess).IsRequired().HasColumnName("is_success");
        builder.Property(e => e.Message).HasMaxLength(1000).HasColumnName("message");
        builder.Property(e => e.ErrorMessage).HasMaxLength(2000).HasColumnName("error_message");
        builder.Property(e => e.MessageId).HasMaxLength(200).HasColumnName("message_id");
        builder.Property(e => e.CreatedTimestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
        builder.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
        builder.HasIndex(e => e.FromAddress).HasDatabaseName("ix_email_logs_from_address");
        builder.HasIndex(e => e.MessageId).HasDatabaseName("ix_email_logs_message_id");
        builder.HasIndex(e => e.CreatedTimestamp).HasDatabaseName("ix_email_logs_created_timestamp");
        builder.HasIndex(e => e.IsSuccess).HasDatabaseName("ix_email_logs_is_success");
    }
}