using Lyo.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Lyo.Geolocation.Postgres.Database;

/// <summary>Design-time factory for creating GeolocationDbContext instances for migrations.</summary>
public class GeolocationDbContextFactory : IDesignTimeDbContextFactory<GeolocationDbContext>
{
    public GeolocationDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("GEOLOCATION_CONNECTION_STRING");
        OperationHelpers.ThrowIfNullOrWhiteSpace(connectionString, "GEOLOCATION_CONNECTION_STRING environment variable must be set for design-time operations.");
        var optionsBuilder = new DbContextOptionsBuilder<GeolocationDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", PostgresGeolocationOptions.Schema));
        return new(optionsBuilder.Options);
    }
}
