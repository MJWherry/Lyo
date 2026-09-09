using Lyo.Postgres;
using Microsoft.EntityFrameworkCore.Design;

namespace Lyo.Geolocation.Postgres.Database;

/// <summary>Design-time factory that mints <see cref="GeolocationDbContext" /> instances for migrations.</summary>
public class GeolocationDbContextFactory : IDesignTimeDbContextFactory<GeolocationDbContext>
{
    /// <inheritdoc />
    public GeolocationDbContext CreateDbContext(string[] args)
        => new(PostgresDesignTime.CreateOptions<GeolocationDbContext>("GEOLOCATION_CONNECTION_STRING", PostgresGeolocationOptions.Schema));
}