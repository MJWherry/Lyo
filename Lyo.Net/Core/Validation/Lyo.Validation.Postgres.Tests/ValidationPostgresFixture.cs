using Lyo.Testing.Containers;
using Lyo.Validation.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Validation.Postgres.Tests;

/// <summary>Shared Postgres container with validation migrations applied.</summary>
public sealed class ValidationPostgresFixture : PostgresContainerFixtureBase
{
    /// <summary>Root provider after the container starts.</summary>
    public IServiceProvider ServiceProvider { get; private set; } = null!;

    /// <inheritdoc />
    protected override async ValueTask OnContainerStartedAsync(string connectionString, CancellationToken cancellationToken)
    {
        var services = new ServiceCollection();
        services.AddLogging(b => {
            b.AddConsole();
            b.SetMinimumLevel(LogLevel.Debug);
        });
        services.AddPostgresValidationStore(new PostgresValidationOptions { ConnectionString = connectionString, EnableAutoMigrations = true });
        ServiceProvider = services.BuildServiceProvider();
        using var scope = ServiceProvider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ValidationDbContext>>();
        await using var context = factory.CreateDbContext();
        await context.Database.MigrateAsync(cancellationToken);
    }

    /// <inheritdoc />
    protected override ValueTask OnContainerDisposingAsync(CancellationToken cancellationToken)
    {
        if (ServiceProvider is IDisposable d)
            d.Dispose();

        return ValueTask.CompletedTask;
    }
}
