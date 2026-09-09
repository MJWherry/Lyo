using Lyo.Postgres;
using Microsoft.EntityFrameworkCore.Design;

namespace Lyo.FileSystemWatcher.Postgres.Database;

/// <summary>EF migrations factory for design-time <c>dotnet ef</c>.</summary>
public sealed class FileSystemWatcherDbContextFactory : IDesignTimeDbContextFactory<FileSystemWatcherDbContext>
{
    /// <inheritdoc />
    public FileSystemWatcherDbContext CreateDbContext(string[] args)
        => new(PostgresDesignTime.CreateOptions<FileSystemWatcherDbContext>(
            "FILESYSTEM_WATCHER_CONNECTION_STRING", PostgresFileSystemWatcherOptions.Schema, "Host=localhost;Database=lyo;Username=lyo;Password=lyo"));
}
