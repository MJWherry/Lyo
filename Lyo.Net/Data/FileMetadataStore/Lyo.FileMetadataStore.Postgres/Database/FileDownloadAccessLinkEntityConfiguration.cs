using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.FileMetadataStore.Postgres.Database;

public sealed class FileDownloadAccessLinkEntityConfiguration : IEntityTypeConfiguration<FileDownloadAccessLinkEntity>
{
    public void Configure(EntityTypeBuilder<FileDownloadAccessLinkEntity> builder)
    {
        builder.ToTable("file_download_access_links");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).IsRequired().HasColumnType("uuid").HasColumnName("id");
        builder.Property(e => e.FileId).IsRequired().HasColumnType("uuid").HasColumnName("file_id");
        builder.Property(e => e.TokenHash).IsRequired().HasColumnType("bytea").HasColumnName("token_hash");
        builder.Property(e => e.CreatedUtc).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_utc");
        builder.Property(e => e.NotBeforeUtc).HasColumnType("timestamp with time zone").HasColumnName("not_before_utc");
        builder.Property(e => e.ExpiresAtUtc).HasColumnType("timestamp with time zone").HasColumnName("expires_at_utc");
        builder.Property(e => e.WindowStartUtc).HasColumnType("timestamp with time zone").HasColumnName("window_start_utc");
        builder.Property(e => e.WindowEndUtc).HasColumnType("timestamp with time zone").HasColumnName("window_end_utc");
        builder.Property(e => e.MaxDownloads).HasColumnType("integer").HasColumnName("max_downloads");
        builder.Property(e => e.DownloadCount).IsRequired().HasDefaultValue(0).HasColumnType("integer").HasColumnName("download_count");
        builder.Property(e => e.LastConsumedUtc).HasColumnType("timestamp with time zone").HasColumnName("last_consumed_utc");
        builder.Property(e => e.IsRevoked).IsRequired().HasDefaultValue(false).HasColumnName("is_revoked");
        builder.Property(e => e.RevokedUtc).HasColumnType("timestamp with time zone").HasColumnName("revoked_utc");
        builder.Property(e => e.TenantId).HasMaxLength(256).HasColumnName("tenant_id");

        builder.HasIndex(e => e.TokenHash).IsUnique().HasDatabaseName("ix_file_download_access_links_token_hash");
        builder.HasIndex(e => e.FileId).HasDatabaseName("ix_file_download_access_links_file_id");
        builder.HasIndex(e => e.ExpiresAtUtc).HasDatabaseName("ix_file_download_access_links_expires_at_utc");
        builder.HasIndex(e => new { e.IsRevoked, e.ExpiresAtUtc }).HasDatabaseName("ix_file_download_access_links_revoked_expires");
        builder.HasIndex(e => e.TenantId).HasDatabaseName("ix_file_download_access_links_tenant_id");
    }
}
