using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Lyo.FileMetadataStore.Sqlite.Database;

/// <summary>Design-time factory for creating <see cref="SqliteFileMetadataStoreDbContext" /> instances for migrations.</summary>
public class SqliteFileMetadataStoreDbContextFactory : IDesignTimeDbContextFactory<SqliteFileMetadataStoreDbContext>
{
    public SqliteFileMetadataStoreDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("FILEMETADATASTORE_CONNECTION_STRING") ?? Environment.GetEnvironmentVariable("FILESTORE_CONNECTION_STRING") ??
            "Data Source=./filestore-design.db";

        var optionsBuilder = new DbContextOptionsBuilder<SqliteFileMetadataStoreDbContext>();
        optionsBuilder.UseSqlite(connectionString);
        return new(optionsBuilder.Options);
    }
}
