using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.FileMetadataStore.Sqlite.Database;

public sealed class MultipartUploadSessionEntityConfiguration : IEntityTypeConfiguration<MultipartUploadSessionEntity>
{
    public void Configure(EntityTypeBuilder<MultipartUploadSessionEntity> builder)
    {
        builder.ToTable("multipart_upload_session");
        builder.HasKey(e => e.SessionId);
        builder.Property(e => e.SessionId).IsRequired().HasColumnName("session_id");
        builder.Property(e => e.TenantId).HasMaxLength(256).HasColumnName("tenant_id");
        builder.Property(e => e.CreatedUtc).IsRequired().HasColumnName("created_utc");
        builder.Property(e => e.ExpiresUtc).IsRequired().HasColumnName("expires_utc");
        builder.Property(e => e.TargetFileId).IsRequired().HasColumnName("target_file_id");
        builder.Property(e => e.PathPrefix).HasMaxLength(500).HasColumnName("path_prefix");
        builder.Property(e => e.Compress).IsRequired().HasColumnName("compress");
        builder.Property(e => e.Encrypt).IsRequired().HasColumnName("encrypt");
        builder.Property(e => e.KeyId).HasMaxLength(255).HasColumnName("key_id");
        builder.Property(e => e.OriginalFileName).HasMaxLength(500).HasColumnName("original_file_name");
        builder.Property(e => e.ContentType).HasMaxLength(255).HasColumnName("content_type");
        builder.Property(e => e.Status).IsRequired().HasColumnName("status");
        builder.Property(e => e.ProviderKind).IsRequired().HasColumnName("provider_kind");
        builder.Property(e => e.ProviderState).IsRequired().HasMaxLength(8192).HasColumnName("provider_state");
        builder.Property(e => e.DeclaredContentLength).HasColumnName("declared_content_length");
        builder.Property(e => e.PartSizeBytes).IsRequired().HasColumnName("part_size_bytes");
        builder.HasIndex(e => e.ExpiresUtc).HasDatabaseName("ix_multipart_upload_session_expires_utc");
        builder.HasIndex(e => e.TargetFileId).HasDatabaseName("ix_multipart_upload_session_target_file_id");
    }
}