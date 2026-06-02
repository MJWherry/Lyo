using Lyo.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lyo.Sqlite;

/// <summary>Hosted service that runs EF Core migrations at application startup when <see cref="ISqliteMigrationConfig.EnableAutoMigrations" /> is true.</summary>
/// <typeparam name="TContext">The DbContext type.</typeparam>
/// <typeparam name="TOptions">The options type implementing <see cref="ISqliteMigrationConfig" />.</typeparam>
public sealed class SqliteMigrationHostedService<TContext, TOptions> : IHostedService
    where TContext : DbContext where TOptions : class, ISqliteMigrationConfig
{
    private readonly ILogger<SqliteMigrationHostedService<TContext, TOptions>> _logger;
    private readonly IServiceProvider _services;

    /// <summary>Creates a new <see cref="SqliteMigrationHostedService{TContext,TOptions}" />.</summary>
    public SqliteMigrationHostedService(IServiceProvider services, ILogger<SqliteMigrationHostedService<TContext, TOptions>> logger)
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
        var options = scope.ServiceProvider.GetRequiredService<IOptions<TOptions>>().Value;
        if (!options.EnableAutoMigrations)
            return;

        ArgumentHelpers.ThrowIfNullOrWhiteSpace(options.ConnectionString);
        _logger.LogInformation("Running SQLite migrations for {Context}", typeof(TContext).Name);
        var dbContextOptions = new DbContextOptionsBuilder<TContext>().UseSqlite(options.ConnectionString).Options;
        await using var context = (TContext)Activator.CreateInstance(typeof(TContext), dbContextOptions)!;
        await context.Database.MigrateAsync(ct).ConfigureAwait(false);
        _logger.LogInformation("SQLite migrations completed for {Context}", typeof(TContext).Name);
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
