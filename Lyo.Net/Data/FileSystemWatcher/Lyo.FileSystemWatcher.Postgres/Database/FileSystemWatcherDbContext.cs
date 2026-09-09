using Microsoft.EntityFrameworkCore;

namespace Lyo.FileSystemWatcher.Postgres.Database;

/// <summary>EF Core context for the <c>filesystem_watcher</c> schema.</summary>
public sealed class FileSystemWatcherDbContext : DbContext
{
    public DbSet<FileSystemWatchEntity> Watches { get; set; } = null!;

    public DbSet<FileSystemSnapshotEntity> Snapshots { get; set; } = null!;

    public DbSet<FileSystemChangeEntity> Changes { get; set; } = null!;

    public FileSystemWatcherDbContext(DbContextOptions<FileSystemWatcherDbContext> options)
        : base(options) { }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(PostgresFileSystemWatcherOptions.Schema);
        modelBuilder.ApplyConfiguration(new FileSystemWatchEntityConfiguration());
        modelBuilder.ApplyConfiguration(new FileSystemSnapshotEntityConfiguration());
        modelBuilder.ApplyConfiguration(new FileSystemChangeEntityConfiguration());
    }
}
