using Lyo.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Lyo.FileMetadataStore.Postgres.Database;

/// <summary>Design-time factory for creating FileMetadataStoreDbContext instances for migrations.</summary>
public class FileMetadataStoreDbContextFactory : IDesignTimeDbContextFactory<FileMetadataStoreDbContext>
{
    public FileMetadataStoreDbContext CreateDbContext(string[] args)
    {
        // Connection string must be provided via environment variable for design-time operations
        var connectionString = Environment.GetEnvironmentVariable("FILEMETADATASTORE_CONNECTION_STRING") ?? Environment.GetEnvironmentVariable("FILESTORE_CONNECTION_STRING");
        OperationHelpers.ThrowIfNullOrWhiteSpace(
            connectionString, "FILEMETADATASTORE_CONNECTION_STRING or FILESTORE_CONNECTION_STRING environment variable must be set for design-time operations.");

        var optionsBuilder = new DbContextOptionsBuilder<FileMetadataStoreDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "filestore"));
        return new(optionsBuilder.Options);
    }
}