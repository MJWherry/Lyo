using Lyo.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
namespace Lyo.Postgres;

/// <summary>Applies EF Core migrations on host start when EnableAutoMigrations is on.</summary>
/// <typeparam name="TContext">DbContext to migrate.</typeparam>
/// <typeparam name="TOptions">Options type that implements <see cref="IPostgresMigrationConfig" />.</typeparam>
public sealed class PostgresMigrationHostedService<TContext, TOptions> : IHostedService
    where TContext : DbContext where TOptions : class, IPostgresMigrationConfig
{
    private readonly ILogger<PostgresMigrationHostedService<TContext, TOptions>> _logger;
    private readonly IServiceProvider _services;

    /// <summary>Builds a <see cref="PostgresMigrationHostedService{TContext,TOptions}" />.</summary>
    public PostgresMigrationHostedService(IServiceProvider services, ILogger<PostgresMigrationHostedService<TContext, TOptions>> logger)
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNull(logger);
        _services = services;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<TOptions>();
        if (!options.EnableAutoMigrations)
            return;

        ArgumentHelpers.ThrowIfNullOrWhiteSpace(options.ConnectionString);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(options.Schema);
        _logger.LogInformation("Running PostgreSQL migrations for {Context} (schema: {Schema})", typeof(TContext).Name, options.Schema);

        await using var context = PostgresSchema.CreateContext<TContext>(options.ConnectionString, options.Schema);
        await PostgresSchema.EnsureAsync(context, options.Schema, ct).ConfigureAwait(false);
        await context.Database.MigrateAsync(ct).ConfigureAwait(false);
        _logger.LogInformation("PostgreSQL migrations completed for {Context}", typeof(TContext).Name);
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}