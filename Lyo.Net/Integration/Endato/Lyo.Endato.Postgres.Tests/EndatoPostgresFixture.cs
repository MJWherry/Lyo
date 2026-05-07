using Lyo.Endato.Postgres.Database;
using Lyo.Testing.Containers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Endato.Postgres.Tests;

public sealed class EndatoPostgresFixture : PostgresContainerFixtureBase
{
    public IServiceProvider ServiceProvider { get; private set; } = null!;

    protected override async ValueTask OnContainerStartedAsync(string connectionString, CancellationToken cancellationToken)
    {
        var services = new ServiceCollection();
        services.AddLogging(b => {
            b.AddConsole();
            b.SetMinimumLevel(LogLevel.Debug);
        });

        services.AddEndatoDbContextFactory(new PostgresEndatoOptions { ConnectionString = connectionString, EnableAutoMigrations = true });
        ServiceProvider = services.BuildServiceProvider();
        using var scope = ServiceProvider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<EndatoDbContext>>();
        await using var context = await factory.CreateDbContextAsync(cancellationToken);
        await context.Database.MigrateAsync(cancellationToken);
    }

    protected override ValueTask OnContainerDisposingAsync(CancellationToken cancellationToken)
    {
        if (ServiceProvider is IDisposable d)
            d.Dispose();

        return ValueTask.CompletedTask;
    }
}