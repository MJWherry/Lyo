using Lyo.FileStorage.Multipart;
using Lyo.FileStorage.Staged;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.FileMetadataStore.Sqlite.Database;

public sealed class StagedFileUploadEntityConfiguration : IEntityTypeConfiguration<StagedFileUploadEntity>
{
    public void Configure(EntityTypeBuilder<StagedFileUploadEntity> builder)
    {
        builder.ToTable("staged_file_upload");
        builder.HasKey(e => e.StageId);
        builder.Property(e => e.StageId).IsRequired().HasColumnName("stage_id");
        builder.Property(e => e.TenantId).HasMaxLength(256).HasColumnName("tenant_id");
        builder.Property(e => e.OwnerId).HasColumnName("owner_id");
        builder.Property(e => e.CreatedUtc).IsRequired().HasColumnName("created_utc");
        builder.Property(e => e.ExpiresUtc).IsRequired().HasColumnName("expires_utc");
        builder.Property(e => e.Status).IsRequired().HasMaxLength(32).HasColumnName("status")
            .HasConversion(v => v.ToString(), v => Enum.Parse<StagedUploadStatus>(v));
        builder.Property(e => e.StorageLocation).IsRequired().HasMaxLength(1024).HasColumnName("storage_location");
        builder.Property(e => e.PathPrefix).HasMaxLength(500).HasColumnName("path_prefix");
        builder.Property(e => e.OriginalFileName).HasMaxLength(500).HasColumnName("original_file_name");
        builder.Property(e => e.ContentType).HasMaxLength(255).HasColumnName("content_type");
        builder.Property(e => e.DeclaredMaxSizeBytes).IsRequired().HasColumnName("declared_max_size_bytes");
        builder.Property(e => e.ObservedSizeBytes).HasColumnName("observed_size_bytes");
        builder.Property(e => e.ContentHash).HasColumnName("content_hash");
        builder.Property(e => e.HashAlgorithm).HasMaxLength(32).HasColumnName("hash_algorithm");
        builder.Property(e => e.ProviderKind).IsRequired().HasMaxLength(32).HasColumnName("provider_kind")
            .HasConversion(v => v.ToString(), v => Enum.Parse<MultipartUploadProviderKind>(v));
        builder.Property(e => e.ProviderState).IsRequired().HasMaxLength(8192).HasColumnName("provider_state");
        builder.Property(e => e.CommittedFileId).HasColumnName("committed_file_id");
        builder.Property(e => e.FailureReason).HasMaxLength(512).HasColumnName("failure_reason");
        builder.HasIndex(e => new { e.TenantId, e.CreatedUtc }).HasDatabaseName("ix_staged_file_upload_tenant_created");
        builder.HasIndex(e => new { e.Status, e.ExpiresUtc }).HasDatabaseName("ix_staged_file_upload_status_expires");
    }
}
