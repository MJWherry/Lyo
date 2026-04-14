using Lyo.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Lyo.ShortUrl.Postgres.Database;

/// <summary>Design-time factory for creating ShortUrlDbContext instances for migrations.</summary>
public class ShortUrlDbContextFactory : IDesignTimeDbContextFactory<ShortUrlDbContext>
{
    public ShortUrlDbContext CreateDbContext(string[] args)
    {
        // Connection string must be provided via environment variable for design-time operations
        var connectionString = Environment.GetEnvironmentVariable("SHORTURL_CONNECTION_STRING");
        OperationHelpers.ThrowIfNullOrWhiteSpace(connectionString, "SHORTURL_CONNECTION_STRING environment variable must be set for design-time operations.");
        var optionsBuilder = new DbContextOptionsBuilder<ShortUrlDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "url"));
        return new(optionsBuilder.Options);
    }
}