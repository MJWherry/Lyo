using Lyo.Exceptions;
using Lyo.Postgres;
using Lyo.Sms.Twilio.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lyo.Sms.Twilio.Postgres;

/// <summary>Extension methods for PostgreSQL Twilio SMS logging database context registration.</summary>
public static class Extensions
{
    /// <summary>Adds TwilioSmsDbContext to the service collection.</summary>
    /// <param name="services">The service collection</param>
    /// <param name="connectionString">The PostgreSQL connection string</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddTwilioSmsDbContext(this IServiceCollection services, string connectionString)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString, nameof(connectionString));
        return services.AddTwilioSmsDbContextFactory(new PostgresTwilioSmsOptions { ConnectionString = connectionString })
            .AddScoped<TwilioSmsDbContext>(sp => sp.GetRequiredService<IDbContextFactory<TwilioSmsDbContext>>().CreateDbContext());
    }

    /// <summary>Adds PostgreSQL Twilio SMS DbContextFactory to the service collection.</summary>
    public static IServiceCollection AddTwilioSmsDbContextFactory(this IServiceCollection services, Action<PostgresTwilioSmsOptions> configure)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
        var options = new PostgresTwilioSmsOptions();
        configure(options);
        return services.AddTwilioSmsDbContextFactory(options);
    }

    /// <summary>Adds PostgreSQL Twilio SMS DbContextFactory using configuration binding.</summary>
    public static IServiceCollection AddTwilioSmsDbContextFactoryFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = PostgresTwilioSmsOptions.SectionName)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configuration, nameof(configuration));
        var options = new PostgresTwilioSmsOptions();
        var section = configuration.GetSection(configSectionName);
        if (section.Exists())
            section.Bind(options);

        return services.AddTwilioSmsDbContextFactory(options);
    }

    /// <summary>Adds PostgreSQL Twilio SMS DbContextFactory to the service collection.</summary>
    public static IServiceCollection AddTwilioSmsDbContextFactory(this IServiceCollection services, PostgresTwilioSmsOptions options)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(options, nameof(options));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(options.ConnectionString, nameof(options.ConnectionString));
        services.AddSingleton<IOptions<PostgresTwilioSmsOptions>>(Options.Create(options));
        services.AddPostgresMigrations<TwilioSmsDbContext, PostgresTwilioSmsOptions>();
        services.AddDbContextFactory<TwilioSmsDbContext>(dbOptions => dbOptions.UseNpgsql(
            options.ConnectionString, npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", PostgresTwilioSmsOptions.Schema)));

        return services;
    }

    /// <summary>Adds TwilioSmsDbContext to the service collection.</summary>
    /// <param name="services">The service collection</param>
    /// <param name="configure">Action to configure the DbContext options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddTwilioSmsDbContext(this IServiceCollection services, Action<DbContextOptionsBuilder> configure)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
        services.AddDbContext<TwilioSmsDbContext>(configure);
        return services;
    }
}