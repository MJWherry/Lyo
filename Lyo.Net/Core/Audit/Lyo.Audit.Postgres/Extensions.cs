using Lyo.Audit.Postgres.Database;
using Lyo.Exceptions;
using Lyo.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lyo.Audit.Postgres;

/// <summary>Extension methods for PostgreSQL audit database context registration.</summary>
public static class Extensions
{
    /// <summary>Adds AuditDbContext to the service collection.</summary>
    /// <param name="services">The service collection</param>
    /// <param name="connectionString">The PostgreSQL connection string</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddAuditDbContext(this IServiceCollection services, string connectionString)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString, nameof(connectionString));
        return services.AddPostgresAuditRecorder(new PostgresAuditOptions { ConnectionString = connectionString })
            .AddScoped<AuditDbContext>(sp => sp.GetRequiredService<IDbContextFactory<AuditDbContext>>().CreateDbContext());
    }

    /// <summary>Adds AuditDbContext to the service collection.</summary>
    /// <param name="services">The service collection</param>
    /// <param name="configure">Action to configure the DbContext options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddAuditDbContext(this IServiceCollection services, Action<DbContextOptionsBuilder> configure)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
        services.AddDbContext<AuditDbContext>(configure);
        return services;
    }

    /// <summary>Adds PostgreSQL audit DbContextFactory to the service collection (IDbContextFactory only).</summary>
    /// <param name="services">The service collection</param>
    /// <param name="configure">Action to configure the PostgreSQL audit options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddAuditDbContextFactory(this IServiceCollection services, Action<PostgresAuditOptions> configure)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
        var options = new PostgresAuditOptions();
        configure(options);
        return services.AddAuditDbContextFactory(options);
    }

    /// <summary>Adds PostgreSQL audit DbContextFactory to the service collection using configuration binding.</summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration (e.g. builder.Configuration)</param>
    /// <param name="configSectionName">The configuration section name (defaults to PostgresAuditOptions.SectionName)</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddAuditDbContextFactoryFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = PostgresAuditOptions.SectionName)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configuration, nameof(configuration));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName, nameof(configSectionName));
        var options = new PostgresAuditOptions();
        var section = configuration.GetSection(configSectionName);
        if (section.Exists())
            section.Bind(options);

        return services.AddAuditDbContextFactory(options);
    }

    /// <summary>Adds PostgreSQL audit DbContextFactory to the service collection (IDbContextFactory only).</summary>
    /// <param name="services">The service collection</param>
    /// <param name="options">The PostgreSQL audit options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddAuditDbContextFactory(this IServiceCollection services, PostgresAuditOptions options)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(options, nameof(options));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(options.ConnectionString, nameof(options.ConnectionString));
        services.AddSingleton<IOptions<PostgresAuditOptions>>(Options.Create(options));
        services.AddPostgresMigrations<AuditDbContext, PostgresAuditOptions>();
        services.AddDbContextFactory<AuditDbContext>(dbOpts => dbOpts.UseNpgsql(
            options.ConnectionString, npgsqlOpts => npgsqlOpts.MigrationsHistoryTable("__EFMigrationsHistory", PostgresAuditOptions.Schema)));

        return services;
    }

    /// <summary>Adds PostgreSQL audit DbContextFactory and PostgresAuditRecorder (IAuditRecorder) to the service collection.</summary>
    /// <param name="services">The service collection</param>
    /// <param name="configure">Action to configure the PostgreSQL audit options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddPostgresAuditRecorder(this IServiceCollection services, Action<PostgresAuditOptions> configure)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
        var options = new PostgresAuditOptions();
        configure(options);
        return services.AddPostgresAuditRecorder(options);
    }

    /// <summary>Adds PostgreSQL audit DbContextFactory and PostgresAuditRecorder to the service collection using configuration binding.</summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration (e.g. builder.Configuration)</param>
    /// <param name="configSectionName">The configuration section name (defaults to PostgresAuditOptions.SectionName)</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddPostgresAuditRecorderFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = PostgresAuditOptions.SectionName)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configuration, nameof(configuration));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName, nameof(configSectionName));
        var options = new PostgresAuditOptions();
        var section = configuration.GetSection(configSectionName);
        if (section.Exists())
            section.Bind(options);

        return services.AddPostgresAuditRecorder(options);
    }

    /// <summary>Adds PostgreSQL audit DbContextFactory and PostgresAuditRecorder to the service collection.</summary>
    /// <param name="services">The service collection</param>
    /// <param name="options">The PostgreSQL audit options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddPostgresAuditRecorder(this IServiceCollection services, PostgresAuditOptions options)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(options, nameof(options));
        services.AddAuditDbContextFactory(options);
        services.AddSingleton<IAuditRecorder, PostgresAuditRecorder>();
        return services;
    }
}