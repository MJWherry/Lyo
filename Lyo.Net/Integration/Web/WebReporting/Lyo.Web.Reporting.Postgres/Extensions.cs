using Lyo.Exceptions;
using Lyo.Postgres;
using Lyo.Web.Reporting.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lyo.Web.Reporting.Postgres;

/// <summary>Extension methods for PostgreSQL reporting service registration.</summary>
public static class Extensions
{
    /// <summary>Adds ReportingDbContext to the service collection.</summary>
    /// <param name="services">The service collection</param>
    /// <param name="connectionString">The PostgreSQL connection string</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddReportingDbContext(this IServiceCollection services, string connectionString)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString, nameof(connectionString));
        return services.AddReportingDbContextFactory(new PostgresReportingOptions { ConnectionString = connectionString })
            .AddScoped<ReportingDbContext>(sp => sp.GetRequiredService<IDbContextFactory<ReportingDbContext>>().CreateDbContext());
    }

    /// <summary>Adds ReportingDbContext to the service collection.</summary>
    /// <param name="services">The service collection</param>
    /// <param name="configure">Action to configure the DbContext options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddReportingDbContext(this IServiceCollection services, Action<DbContextOptionsBuilder> configure)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
        services.AddDbContext<ReportingDbContext>(configure);
        return services;
    }

    /// <summary>Adds PostgreSQL reporting DbContextFactory to the service collection.</summary>
    /// <param name="services">The service collection</param>
    /// <param name="configure">Action to configure the PostgreSQL reporting options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddReportingDbContextFactory(this IServiceCollection services, Action<PostgresReportingOptions> configure)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
        var options = new PostgresReportingOptions();
        configure(options);
        return services.AddReportingDbContextFactory(options);
    }

    /// <summary>Adds PostgreSQL reporting DbContextFactory to the service collection using configuration binding.</summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration (e.g. builder.Configuration)</param>
    /// <param name="configSectionName">The configuration section name (defaults to PostgresReportingOptions.SectionName)</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddReportingDbContextFactoryFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = PostgresReportingOptions.SectionName)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configuration, nameof(configuration));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName, nameof(configSectionName));
        var options = new PostgresReportingOptions();
        var section = configuration.GetSection(configSectionName);
        if (section.Exists())
            section.Bind(options);

        return services.AddReportingDbContextFactory(options);
    }

    /// <summary>Adds PostgreSQL reporting DbContextFactory to the service collection.</summary>
    /// <param name="services">The service collection</param>
    /// <param name="options">The PostgreSQL reporting options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddReportingDbContextFactory(this IServiceCollection services, PostgresReportingOptions options)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(options, nameof(options));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(options.ConnectionString, nameof(options.ConnectionString));
        services.AddSingleton<IOptions<PostgresReportingOptions>>(Options.Create(options));
        services.AddPostgresMigrations<ReportingDbContext, PostgresReportingOptions>();
        services.AddDbContextFactory<ReportingDbContext>(dbOptions => dbOptions.UseNpgsql(
            options.ConnectionString, npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", PostgresReportingOptions.Schema)));

        return services;
    }
}