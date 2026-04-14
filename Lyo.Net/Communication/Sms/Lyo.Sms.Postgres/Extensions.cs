using Lyo.Exceptions;
using Lyo.Postgres;
using Lyo.Sms.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lyo.Sms.Postgres;

/// <summary>Extension methods for PostgreSQL SMS logging database context registration.</summary>
public static class Extensions
{
    /// <summary>Adds SmsDbContext to the service collection.</summary>
    /// <param name="services">The service collection</param>
    /// <param name="connectionString">The PostgreSQL connection string</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddSmsDbContext(this IServiceCollection services, string connectionString)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString, nameof(connectionString));
        return services.AddSmsDbContextFactory(new PostgresSmsOptions { ConnectionString = connectionString })
            .AddScoped<SmsDbContext>(sp => sp.GetRequiredService<IDbContextFactory<SmsDbContext>>().CreateDbContext());
    }

    /// <summary>Adds SmsDbContext to the service collection.</summary>
    /// <param name="services">The service collection</param>
    /// <param name="configure">Action to configure the DbContext options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddSmsDbContext(this IServiceCollection services, Action<DbContextOptionsBuilder> configure)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
        services.AddDbContext<SmsDbContext>(configure);
        return services;
    }

    /// <summary>Adds PostgreSQL SMS logging DbContextFactory to the service collection.</summary>
    /// <param name="services">The service collection</param>
    /// <param name="configure">Action to configure the PostgreSQL SMS logging options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddSmsDbContextFactory(this IServiceCollection services, Action<PostgresSmsOptions> configure)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
        var options = new PostgresSmsOptions();
        configure(options);
        return services.AddSmsDbContextFactory(options);
    }

    /// <summary>Adds PostgreSQL SMS logging DbContextFactory to the service collection using configuration binding.</summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration (e.g. builder.Configuration)</param>
    /// <param name="configSectionName">The configuration section name (defaults to PostgresSmsOptions.SectionName)</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddSmsDbContextFactoryFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = PostgresSmsOptions.SectionName)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configuration, nameof(configuration));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName, nameof(configSectionName));
        var options = new PostgresSmsOptions();
        var section = configuration.GetSection(configSectionName);
        if (section.Exists())
            section.Bind(options);

        return services.AddSmsDbContextFactory(options);
    }

    /// <summary>Adds PostgreSQL SMS logging DbContextFactory to the service collection.</summary>
    /// <param name="services">The service collection</param>
    /// <param name="options">The PostgreSQL SMS logging options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddSmsDbContextFactory(this IServiceCollection services, PostgresSmsOptions options)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(options, nameof(options));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(options.ConnectionString, nameof(options.ConnectionString));
        services.AddSingleton<IOptions<PostgresSmsOptions>>(Options.Create(options));
        services.AddPostgresMigrations<SmsDbContext, PostgresSmsOptions>();
        services.AddDbContextFactory<SmsDbContext>(dbOptions => dbOptions.UseNpgsql(
            options.ConnectionString, npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", PostgresSmsOptions.Schema)));

        return services;
    }
}