using Lyo.Geolocation.Postgres.Database;
using Lyo.Testing.Containers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Geolocation.Postgres.Tests;

public sealed class GeolocationPostgresFixture : PostgresContainerFixtureBase
{
    public IServiceProvider ServiceProvider { get; private set; } = null!;

    protected override async ValueTask OnContainerStartedAsync(string connectionString, CancellationToken cancellationToken)
    {
        var services = new ServiceCollection();
        services.AddLogging(b => {
            b.AddConsole();
            b.SetMinimumLevel(LogLevel.Debug);
        });

        services.AddPostgresGeolocationStore(new PostgresGeolocationOptions { ConnectionString = connectionString, EnableAutoMigrations = true });
        ServiceProvider = services.BuildServiceProvider();
        using var scope = ServiceProvider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<GeolocationDbContext>>();
        await using var context = factory.CreateDbContext();
        await context.Database.MigrateAsync(cancellationToken);
    }

    protected override ValueTask OnContainerDisposingAsync(CancellationToken cancellationToken)
    {
        if (ServiceProvider is IDisposable d)
            d.Dispose();

        return ValueTask.CompletedTask;
    }
}