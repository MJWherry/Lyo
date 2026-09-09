using Lyo.Postgres;
using Microsoft.EntityFrameworkCore.Design;

namespace Lyo.FileMetadataStore.Postgres.Database;

/// <summary>Design-time factory that builds <see cref="FileMetadataStoreDbContext" /> instances for migrations.</summary>
public class FileMetadataStoreDbContextFactory : IDesignTimeDbContextFactory<FileMetadataStoreDbContext>
{
    /// <inheritdoc />
    public FileMetadataStoreDbContext CreateDbContext(string[] args)
        => new(PostgresDesignTime.CreateOptions<FileMetadataStoreDbContext>(["FILEMETADATASTORE_CONNECTION_STRING", "FILESTORE_CONNECTION_STRING"], "filestore"));
}