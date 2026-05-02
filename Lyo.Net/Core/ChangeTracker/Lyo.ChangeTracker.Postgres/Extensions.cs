using Lyo.ChangeTracker.Postgres.Database;
using Lyo.Exceptions;
using Lyo.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lyo.ChangeTracker.Postgres;

/// <summary>Extension methods for PostgreSQL change tracking registration.</summary>
public static class Extensions
{
    /// <summary>Adds ChangeTrackerDbContext to the service collection.</summary>
    public static IServiceCollection AddChangeTrackerDbContext(this IServiceCollection services, string connectionString)
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString);
        return services.AddPostgresChangeTracker(new PostgresChangeTrackerOptions { ConnectionString = connectionString })
            .AddScoped<ChangeTrackerDbContext>(sp => sp.GetRequiredService<IDbContextFactory<ChangeTrackerDbContext>>().CreateDbContext());
    }

    /// <summary>Adds ChangeTrackerDbContext to the service collection.</summary>
    public static IServiceCollection AddChangeTrackerDbContext(this IServiceCollection services, Action<DbContextOptionsBuilder> configure)
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNull(configure);
        services.AddDbContext<ChangeTrackerDbContext>(configure);
        return services;
    }

    /// <summary>Adds PostgreSQL change tracking DbContextFactory to the service collection.</summary>
    public static IServiceCollection AddChangeTrackerDbContextFactory(this IServiceCollection services, Action<PostgresChangeTrackerOptions> configure)
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNull(configure);
        var options = new PostgresChangeTrackerOptions();
        configure(options);
        return services.AddChangeTrackerDbContextFactory(options);
    }

    /// <summary>Adds PostgreSQL change tracking DbContextFactory using configuration binding.</summary>
    public static IServiceCollection AddChangeTrackerDbContextFactoryFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = PostgresChangeTrackerOptions.SectionName)
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNull(configuration);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
        var options = new PostgresChangeTrackerOptions();
        var section = configuration.GetSection(configSectionName);
        if (section.Exists())
            section.Bind(options);

        return services.AddChangeTrackerDbContextFactory(options);
    }

    /// <summary>Adds PostgreSQL change tracking DbContextFactory to the service collection.</summary>
    public static IServiceCollection AddChangeTrackerDbContextFactory(this IServiceCollection services, PostgresChangeTrackerOptions options)
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNull(options);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(options.ConnectionString, nameof(options.ConnectionString));
        services.AddSingleton<IOptions<PostgresChangeTrackerOptions>>(Options.Create(options));
        services.AddPostgresMigrations<ChangeTrackerDbContext, PostgresChangeTrackerOptions>();
        services.AddDbContextFactory<ChangeTrackerDbContext>(dbOpts => dbOpts.UseNpgsql(
            options.ConnectionString, npgsqlOpts => npgsqlOpts.MigrationsHistoryTable("__EFMigrationsHistory", PostgresChangeTrackerOptions.Schema)));

        return services;
    }

    /// <summary>Adds PostgreSQL change tracking services to the service collection.</summary>
    public static IServiceCollection AddPostgresChangeTracker(this IServiceCollection services, Action<PostgresChangeTrackerOptions> configure)
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNull(configure);
        var options = new PostgresChangeTrackerOptions();
        configure(options);
        return services.AddPostgresChangeTracker(options);
    }

    /// <summary>Adds PostgreSQL change tracking services using configuration binding.</summary>
    public static IServiceCollection AddPostgresChangeTrackerFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = PostgresChangeTrackerOptions.SectionName)
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNull(configuration);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
        var options = new PostgresChangeTrackerOptions();
        var section = configuration.GetSection(configSectionName);
        if (section.Exists())
            section.Bind(options);

        return services.AddPostgresChangeTracker(options);
    }

    /// <summary>Adds PostgreSQL change tracking services to the service collection.</summary>
    public static IServiceCollection AddPostgresChangeTracker(this IServiceCollection services, PostgresChangeTrackerOptions options)
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNull(options);
        services.AddChangeTrackerDbContextFactory(options);
        services.AddSingleton<IChangeTracker, PostgresChangeTracker>();
        return services;
    }
}