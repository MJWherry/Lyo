using Lyo.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lyo.Postgres;

/// <summary>Hosted service that runs EF Core migrations at application startup when EnableAutoMigrations is true.</summary>
/// <typeparam name="TContext">The DbContext type.</typeparam>
/// <typeparam name="TOptions">The options type implementing IPostgresMigrationConfig.</typeparam>
public sealed class PostgresMigrationHostedService<TContext, TOptions> : IHostedService
    where TContext : DbContext where TOptions : class, IPostgresMigrationConfig
{
    private readonly ILogger<PostgresMigrationHostedService<TContext, TOptions>> _logger;
    private readonly IServiceProvider _services;

    /// <summary>Creates a new PostgresMigrationHostedService.</summary>
    public PostgresMigrationHostedService(IServiceProvider services, ILogger<PostgresMigrationHostedService<TContext, TOptions>> logger)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(logger, nameof(logger));
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

        ArgumentHelpers.ThrowIfNullOrWhiteSpace(options.ConnectionString, nameof(options.ConnectionString));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(options.Schema, nameof(options.Schema));
        _logger.LogInformation("Running PostgreSQL migrations for {Context} (schema: {Schema})", typeof(TContext).Name, options.Schema);
        var dbContextOptions = new DbContextOptionsBuilder<TContext>().UseNpgsql(
                options.ConnectionString, npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", options.Schema))
            .Options;

        await using var context = (TContext)Activator.CreateInstance(typeof(TContext), dbContextOptions)!;
        var schema = options.Schema.Replace("\"", "\"\"");
#pragma warning disable EF1002 // SQL injection - schema name is from config, validated
        await context.Database.ExecuteSqlRawAsync($"CREATE SCHEMA IF NOT EXISTS \"{schema}\";", ct).ConfigureAwait(false);
#pragma warning restore EF1002
        await context.Database.MigrateAsync(ct).ConfigureAwait(false);
        _logger.LogInformation("PostgreSQL migrations completed for {Context}", typeof(TContext).Name);
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}