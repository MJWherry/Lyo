using Lyo.HomeInventory.Postgres.Database;
using Lyo.Testing.Containers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.HomeInventory.Postgres.Tests;

/// <summary>Testcontainers fixture for the home_inventory schema.</summary>
public sealed class HomeInventoryPostgresFixture : PostgresServiceFixtureBase<HomeInventoryDbContext>
{
    /// <inheritdoc />
    protected override LogLevel MinimumLogLevel => LogLevel.Warning;

    /// <inheritdoc />
    protected override void ConfigureServices(IServiceCollection services, string connectionString)
        => services.AddPostgresHomeInventoryStore(new PostgresHomeInventoryOptions { ConnectionString = connectionString, EnableAutoMigrations = true });
}