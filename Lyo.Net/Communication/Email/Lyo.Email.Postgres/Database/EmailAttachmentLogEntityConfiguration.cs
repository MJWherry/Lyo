using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Email.Postgres.Database;

/// <summary>Fluent mapping for <see cref="EmailAttachmentLogEntity" />.</summary>
public sealed class EmailAttachmentLogEntityConfiguration : IEntityTypeConfiguration<EmailAttachmentLogEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<EmailAttachmentLogEntity> builder)
    {
        builder.ToTable("email_attachment_logs");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).IsRequired().HasColumnName("id");
        builder.Property(e => e.EmailLogId).IsRequired().HasColumnName("email_log_id");
        builder.Property(e => e.FileName).HasMaxLength(500).IsRequired().HasColumnName("file_name");
        builder.Property(e => e.ContentType).HasMaxLength(100).HasColumnName("content_type");
        builder.Property(e => e.SizeBytes).IsRequired().HasColumnName("size_bytes");
        builder.Property(e => e.MetadataJson).HasColumnType("jsonb").HasColumnName("metadata_json");
        builder.Property(e => e.SortOrder).IsRequired().HasColumnName("sort_order");
        builder.Property(e => e.CreatedTimestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
        builder.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
        builder.HasOne(e => e.EmailLog).WithMany(e => e.Attachments).HasForeignKey(e => e.EmailLogId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => e.EmailLogId).HasDatabaseName("ix_email_attachment_logs_email_log_id");
    }
}
