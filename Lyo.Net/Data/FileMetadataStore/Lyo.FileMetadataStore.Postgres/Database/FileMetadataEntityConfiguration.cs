using Lyo.FileMetadataStore.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.FileMetadataStore.Postgres.Database;

public class FileMetadataEntityConfiguration : IEntityTypeConfiguration<FileMetadataEntity>
{
    public void Configure(EntityTypeBuilder<FileMetadataEntity> builder)
    {
        builder.ToTable("file_metadata");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).IsRequired().HasColumnType("uuid").HasConversion(v => Guid.Parse(v), v => v.ToString()).HasColumnName("id");
        builder.Property(e => e.OriginalFileName).HasMaxLength(500).HasColumnName("original_file_name");
        builder.Property(e => e.OriginalFileSize).IsRequired().HasColumnName("original_file_size");
        builder.Property(e => e.OriginalFileHash).IsRequired().HasColumnType("bytea").HasColumnName("original_file_hash");
        builder.Property(e => e.SourceFileName).HasMaxLength(500).IsRequired().HasColumnName("source_file_name");
        builder.Property(e => e.SourceFileSize).IsRequired().HasColumnName("source_file_size");
        builder.Property(e => e.SourceFileHash).IsRequired().HasColumnType("bytea").HasColumnName("source_file_hash");
        builder.Property(e => e.IsCompressed).IsRequired().HasDefaultValue(false).HasColumnName("is_compressed");
        builder.Property(e => e.CompressionAlgorithm).HasMaxLength(50).HasColumnName("compression_algorithm");
        builder.Property(e => e.CompressedFileSize).HasColumnName("compressed_file_size");
        builder.Property(e => e.CompressedFileHash).HasColumnType("bytea").HasColumnName("compressed_file_hash");
        builder.Property(e => e.IsEncrypted).IsRequired().HasDefaultValue(false).HasColumnName("is_encrypted");
        builder.Property(e => e.DataEncryptionKeyAlgorithm).HasMaxLength(50).HasColumnName("data_encryption_key_algorithm");
        builder.Property(e => e.KeyEncryptionKeyAlgorithm).HasMaxLength(50).HasColumnName("key_encryption_key_algorithm");
        builder.Property(e => e.EncryptedFileSize).HasColumnName("encrypted_file_size");
        builder.Property(e => e.EncryptedFileHash).HasColumnType("bytea").HasColumnName("encrypted_file_hash");
        builder.Property(e => e.EncryptedDataEncryptionKey).HasColumnType("bytea").HasColumnName("encrypted_data_encryption_key");
        builder.Property(e => e.DataEncryptionKeyId).HasMaxLength(255).HasColumnName("data_encryption_key_id");
        builder.Property(e => e.DataEncryptionKeyVersion).HasMaxLength(255).HasColumnName("data_encryption_key_version");
        builder.Property(e => e.KeyEncryptionKeySalt).HasColumnType("bytea").HasColumnName("key_encryption_key_salt");
        builder.Property(e => e.DekKeyMaterialBytes).HasColumnType("smallint").HasColumnName("dek_key_material_bytes");
        builder.Property(e => e.Timestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("timestamp");
        builder.Property(e => e.PathPrefix).HasMaxLength(500).HasColumnName("path_prefix");
        builder.Property(e => e.HashAlgorithm).HasMaxLength(20).HasColumnName("hash_algorithm");
        builder.Property(e => e.ContentType).HasMaxLength(255).HasColumnName("content_type");
        builder.Property(e => e.TenantId).HasMaxLength(256).HasColumnName("tenant_id");
        builder.Property(e => e.Availability).HasMaxLength(32).HasColumnName("availability");

        // Create indexes
        builder.HasIndex(e => e.OriginalFileHash).HasDatabaseName("ix_file_metadata_original_file_hash");
        builder.HasIndex(e => e.Timestamp).HasDatabaseName("ix_file_metadata_timestamp");
        builder.HasIndex(e => e.OriginalFileName).HasDatabaseName("ix_file_metadata_original_file_name");
        builder.HasIndex(e => e.TenantId).HasDatabaseName("ix_file_metadata_tenant_id");
    }
}