using Lyo.ChangeTracker.Postgres.Database;
using Lyo.Testing.Containers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.ChangeTracker.Postgres.Tests;

public sealed class ChangeTrackerPostgresFixture : PostgresContainerFixtureBase
{
    public IServiceProvider ServiceProvider { get; private set; } = null!;

    public IChangeTracker ChangeTracker { get; private set; } = null!;

    protected override async ValueTask OnContainerStartedAsync(string connectionString, CancellationToken cancellationToken)
    {
        var services = new ServiceCollection();
        services.AddLogging(b => {
            b.AddConsole();
            b.SetMinimumLevel(LogLevel.Debug);
        });

        services.AddDbContextFactory<ChangeTrackerDbContext>(opts => opts.UseNpgsql(
            connectionString, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", PostgresChangeTrackerOptions.Schema)));

        ServiceProvider = services.BuildServiceProvider();
        using var scope = ServiceProvider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ChangeTrackerDbContext>>();
        await using var context = await factory.CreateDbContextAsync(cancellationToken);
        await context.Database.MigrateAsync(cancellationToken);
        var trackerFactory = ServiceProvider.GetRequiredService<IDbContextFactory<ChangeTrackerDbContext>>();
        ChangeTracker = new PostgresChangeTracker(trackerFactory);
    }

    protected override ValueTask OnContainerDisposingAsync(CancellationToken cancellationToken)
    {
        if (ServiceProvider is IDisposable d)
            d.Dispose();

        return ValueTask.CompletedTask;
    }
}