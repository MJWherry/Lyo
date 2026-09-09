using Lyo.Postgres;
using Microsoft.EntityFrameworkCore.Design;

namespace Lyo.ShortUrl.Postgres.Database;

/// <summary>EF migrations factory that builds ShortUrlDbContext instances for migrations.</summary>
public class ShortUrlDbContextFactory : IDesignTimeDbContextFactory<ShortUrlDbContext>
{
    /// <inheritdoc />
    public ShortUrlDbContext CreateDbContext(string[] args) => new(PostgresDesignTime.CreateOptions<ShortUrlDbContext>("SHORTURL_CONNECTION_STRING", "url"));
}