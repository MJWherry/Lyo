using Lyo.Email.Postgres.Database;
using Lyo.Exceptions;
using Lyo.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lyo.Email.Postgres;

/// <summary>Extension methods for PostgreSQL email logging.</summary>
public static class Extensions
{
    /// <summary>Adds EmailDbContext to the service collection.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The PostgreSQL connection string.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEmailDbContext(this IServiceCollection services, string connectionString)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString, nameof(connectionString));
        return services.AddEmailDbContextFactory(new PostgresEmailOptions { ConnectionString = connectionString })
            .AddScoped<EmailDbContext>(sp => sp.GetRequiredService<IDbContextFactory<EmailDbContext>>().CreateDbContext());
    }

    /// <summary>Adds EmailDbContext to the service collection.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action to configure the DbContext options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEmailDbContext(this IServiceCollection services, Action<DbContextOptionsBuilder> configure)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
        services.AddDbContext<EmailDbContext>(configure);
        return services;
    }

    /// <summary>Adds PostgreSQL email DbContextFactory and schema. Consumers handle mapping/insertion from EmailSent events.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action to configure the PostgreSQL email logging options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEmailDbContextFactory(this IServiceCollection services, Action<PostgresEmailOptions> configure)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
        var options = new PostgresEmailOptions();
        configure(options);
        return services.AddEmailDbContextFactory(options);
    }

    /// <summary>Adds PostgreSQL email DbContextFactory using configuration binding.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration (e.g. builder.Configuration).</param>
    /// <param name="configSectionName">The configuration section name (defaults to PostgresEmailOptions.SectionName).</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEmailDbContextFactoryFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = PostgresEmailOptions.SectionName)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configuration, nameof(configuration));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName, nameof(configSectionName));
        var options = new PostgresEmailOptions();
        var section = configuration.GetSection(configSectionName);
        if (section.Exists())
            section.Bind(options);

        return services.AddEmailDbContextFactory(options);
    }

    /// <summary>Adds PostgreSQL email DbContextFactory and schema. Consumers handle mapping/insertion from EmailSent events.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">The PostgreSQL email logging options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEmailDbContextFactory(this IServiceCollection services, PostgresEmailOptions options)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(options, nameof(options));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(options.ConnectionString, nameof(options.ConnectionString));
        services.AddSingleton<IOptions<PostgresEmailOptions>>(Options.Create(options));
        services.AddPostgresMigrations<EmailDbContext, PostgresEmailOptions>();
        services.AddDbContextFactory<EmailDbContext>(dbOptions => dbOptions.UseNpgsql(
            options.ConnectionString, npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", PostgresEmailOptions.Schema)));

        return services;
    }
}