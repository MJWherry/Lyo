using Lyo.FileMetadataStore.Models;
using Microsoft.EntityFrameworkCore;

namespace Lyo.FileMetadataStore.Sqlite.Database;

public class SqliteFileMetadataStoreDbContext : DbContext
{
    public DbSet<FileMetadataEntity> FileMetadata { get; set; } = null!;

    public DbSet<FileDataEntity> FileData { get; set; } = null!;

    public DbSet<FileAuditEventEntity> FileAuditEvents { get; set; } = null!;

    public DbSet<MultipartUploadSessionEntity> MultipartUploadSessions { get; set; } = null!;

    public DbSet<FileDownloadAccessLinkEntity> FileDownloadAccessLinks { get; set; } = null!;

    public DbSet<StagedFileUploadEntity> StagedFileUploads { get; set; } = null!;

    public SqliteFileMetadataStoreDbContext(DbContextOptions<SqliteFileMetadataStoreDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new FileMetadataEntityConfiguration());
        modelBuilder.ApplyConfiguration(new FileDataEntityConfiguration());
        modelBuilder.ApplyConfiguration(new FileAuditEventEntityConfiguration());
        modelBuilder.ApplyConfiguration(new MultipartUploadSessionEntityConfiguration());
        modelBuilder.ApplyConfiguration(new FileDownloadAccessLinkEntityConfiguration());
        modelBuilder.ApplyConfiguration(new StagedFileUploadEntityConfiguration());
    }
}