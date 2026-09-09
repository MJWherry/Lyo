using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.FileMetadataStore.Sqlite.Database;

public sealed class FileDownloadAccessLinkEntityConfiguration : IEntityTypeConfiguration<FileDownloadAccessLinkEntity>
{
    public void Configure(EntityTypeBuilder<FileDownloadAccessLinkEntity> builder)
    {
        builder.ToTable("file_download_access_links");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).IsRequired().HasColumnName("id");
        builder.Property(e => e.FileId).IsRequired().HasColumnName("file_id");
        builder.Property(e => e.TokenHash).IsRequired().HasColumnName("token_hash");
        builder.Property(e => e.CreatedUtc).IsRequired().HasColumnName("created_utc");
        builder.Property(e => e.NotBeforeUtc).HasColumnName("not_before_utc");
        builder.Property(e => e.ExpiresAtUtc).HasColumnName("expires_at_utc");
        builder.Property(e => e.WindowStartUtc).HasColumnName("window_start_utc");
        builder.Property(e => e.WindowEndUtc).HasColumnName("window_end_utc");
        builder.Property(e => e.MaxDownloads).HasColumnName("max_downloads");
        builder.Property(e => e.DownloadCount).IsRequired().HasDefaultValue(0).HasColumnName("download_count");
        builder.Property(e => e.LastConsumedUtc).HasColumnName("last_consumed_utc");
        builder.Property(e => e.IsRevoked).IsRequired().HasDefaultValue(false).HasColumnName("is_revoked");
        builder.Property(e => e.RevokedUtc).HasColumnName("revoked_utc");
        builder.Property(e => e.TenantId).HasMaxLength(256).HasColumnName("tenant_id");
        builder.HasIndex(e => e.TokenHash).IsUnique().HasDatabaseName("ix_file_download_access_links_token_hash");
        builder.HasIndex(e => e.FileId).HasDatabaseName("ix_file_download_access_links_file_id");
        builder.HasIndex(e => e.ExpiresAtUtc).HasDatabaseName("ix_file_download_access_links_expires_at_utc");
        builder.HasIndex(e => new { e.IsRevoked, e.ExpiresAtUtc }).HasDatabaseName("ix_file_download_access_links_revoked_expires");
        builder.HasIndex(e => e.TenantId).HasDatabaseName("ix_file_download_access_links_tenant_id");
    }
}