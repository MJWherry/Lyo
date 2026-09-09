using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Testing.Containers;

/// <summary>
/// xUnit fixture base for suites that need a Postgres container plus a built <see cref="IServiceProvider" /> with migrations applied. Subclasses supply only
/// <see cref="ConfigureServices" />; console logging, provider construction, <c>MigrateAsync</c>, and provider disposal live here. Override
/// <see cref="OnMigratedAsync" /> to seed rows or resolve services onto fixture properties, and <see cref="MigrateAsync" /> when the context is registered as a scoped
/// <see cref="DbContext" /> rather than through <see cref="IDbContextFactory{TContext}" />.
/// </summary>
/// <typeparam name="TContext">The <see cref="DbContext" /> whose migrations are applied once the container is up.</typeparam>
public abstract class PostgresServiceFixtureBase<TContext> : PostgresContainerFixtureBase
    where TContext : DbContext
{
    /// <summary>Root provider, usable from <see cref="OnMigratedAsync" /> onward.</summary>
    public IServiceProvider ServiceProvider { get; private set; } = null!;

    /// <summary>The registered context factory. Only valid when <typeparamref name="TContext" /> was registered with <c>AddDbContextFactory</c>.</summary>
    public IDbContextFactory<TContext> ContextFactory => ServiceProvider.GetRequiredService<IDbContextFactory<TContext>>();

    /// <summary>Minimum level for the console logger attached to the fixture provider.</summary>
    protected virtual LogLevel MinimumLogLevel => LogLevel.Debug;

    protected PostgresServiceFixtureBase()
        : this(null) { }

    protected PostgresServiceFixtureBase(PostgresContainerOptions? options)
        : base(options) { }

    /// <summary>Opens a scope on the fixture provider.</summary>
    public IServiceScope CreateScope() => ServiceProvider.CreateScope();

    /// <summary>Registers the services under test. The connection string targets the running container.</summary>
    protected abstract void ConfigureServices(IServiceCollection services, string connectionString);

    /// <summary>Applies migrations for <typeparamref name="TContext" />. Override when the context is scoped instead of factory-registered.</summary>
    protected virtual async ValueTask MigrateAsync(CancellationToken cancellationToken)
    {
        using var scope = CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<TContext>>();
        await using var context = await factory.CreateDbContextAsync(cancellationToken);
        await context.Database.MigrateAsync(cancellationToken);
    }

    /// <summary>Called after migrations have been applied — seed data or resolve services here.</summary>
    protected virtual ValueTask OnMigratedAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;

    /// <inheritdoc />
    protected sealed override async ValueTask OnContainerStartedAsync(string connectionString, CancellationToken cancellationToken)
    {
        var services = new ServiceCollection();
        services.AddLogging(b => {
            b.AddConsole();
            b.SetMinimumLevel(MinimumLogLevel);
        });

        ConfigureServices(services, connectionString);
        ServiceProvider = services.BuildServiceProvider();
        await MigrateAsync(cancellationToken);
        await OnMigratedAsync(cancellationToken);
    }

    /// <inheritdoc />
    protected override ValueTask OnContainerDisposingAsync(CancellationToken cancellationToken)
    {
        if (ServiceProvider is IDisposable d)
            d.Dispose();

        return ValueTask.CompletedTask;
    }
}
