using Lyo.FileMetadataStore.Models;
using Microsoft.EntityFrameworkCore;

namespace Lyo.FileMetadataStore.Postgres.Database;

public class FileMetadataStoreDbContext : DbContext
{
    public DbSet<FileMetadataEntity> FileMetadata { get; set; } = null!;

    public DbSet<FileDataEntity> FileData { get; set; } = null!;

    public DbSet<FileAuditEventEntity> FileAuditEvents { get; set; } = null!;

    public DbSet<MultipartUploadSessionEntity> MultipartUploadSessions { get; set; } = null!;

    public FileMetadataStoreDbContext(DbContextOptions<FileMetadataStoreDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("filestore");
        modelBuilder.ApplyConfiguration(new FileMetadataEntityConfiguration());
        modelBuilder.ApplyConfiguration(new FileDataEntityConfiguration());
        modelBuilder.ApplyConfiguration(new FileAuditEventEntityConfiguration());
        modelBuilder.ApplyConfiguration(new MultipartUploadSessionEntityConfiguration());
    }
}