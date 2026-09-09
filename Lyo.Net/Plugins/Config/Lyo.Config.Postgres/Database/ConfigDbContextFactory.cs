using Lyo.Postgres;
using Microsoft.EntityFrameworkCore.Design;

namespace Lyo.Config.Postgres.Database;

/// <summary>EF migrations factory that builds ConfigDbContext instances for migrations.</summary>
public class ConfigDbContextFactory : IDesignTimeDbContextFactory<ConfigDbContext>
{
    /// <inheritdoc />
    public ConfigDbContext CreateDbContext(string[] args) => new(PostgresDesignTime.CreateOptions<ConfigDbContext>("CONFIG_CONNECTION_STRING", "config"));
}