using Lyo.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Lyo.Config.Postgres.Database;

/// <summary>Design-time factory for creating ConfigDbContext instances for migrations.</summary>
public class ConfigDbContextFactory : IDesignTimeDbContextFactory<ConfigDbContext>
{
    public ConfigDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("CONFIG_CONNECTION_STRING");
        OperationHelpers.ThrowIfNullOrWhiteSpace(connectionString, "CONFIG_CONNECTION_STRING environment variable must be set for design-time operations.");
        var optionsBuilder = new DbContextOptionsBuilder<ConfigDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "config"));
        return new(optionsBuilder.Options);
    }
}