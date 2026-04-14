using Lyo.Exceptions;
using Lyo.People.Postgres.Database;
using Lyo.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lyo.People.Postgres;

/// <summary>Extension methods for PostgreSQL People database context registration.</summary>
public static class Extensions
{
    /// <summary>Adds PeopleDbContext to the service collection.</summary>
    /// <param name="services">The service collection</param>
    /// <param name="connectionString">The PostgreSQL connection string</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddPeopleDbContext(this IServiceCollection services, string connectionString)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString, nameof(connectionString));
        return services.AddPeopleDbContextFactory(new PostgresPeopleOptions { ConnectionString = connectionString })
            .AddScoped<PeopleDbContext>(sp => sp.GetRequiredService<IDbContextFactory<PeopleDbContext>>().CreateDbContext());
    }

    /// <summary>Adds PeopleDbContext to the service collection.</summary>
    /// <param name="services">The service collection</param>
    /// <param name="configure">Action to configure the DbContext options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddPeopleDbContext(this IServiceCollection services, Action<DbContextOptionsBuilder> configure)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
        services.AddDbContext<PeopleDbContext>(configure);
        return services;
    }

    /// <summary>Adds PostgreSQL People DbContextFactory to the service collection.</summary>
    /// <param name="services">The service collection</param>
    /// <param name="configure">Action to configure the PostgreSQL People options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddPeopleDbContextFactory(this IServiceCollection services, Action<PostgresPeopleOptions> configure)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
        var options = new PostgresPeopleOptions();
        configure(options);
        return services.AddPeopleDbContextFactory(options);
    }

    /// <summary>Adds PostgreSQL People DbContextFactory to the service collection using configuration binding.</summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration (e.g. builder.Configuration)</param>
    /// <param name="configSectionName">The configuration section name (defaults to PostgresPeopleOptions.SectionName)</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddPeopleDbContextFactoryFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = PostgresPeopleOptions.SectionName)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configuration, nameof(configuration));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName, nameof(configSectionName));
        var options = new PostgresPeopleOptions();
        var section = configuration.GetSection(configSectionName);
        if (section.Exists())
            section.Bind(options);

        return services.AddPeopleDbContextFactory(options);
    }

    /// <summary>Adds PostgreSQL People DbContextFactory to the service collection.</summary>
    /// <param name="services">The service collection</param>
    /// <param name="options">The PostgreSQL People options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddPeopleDbContextFactory(this IServiceCollection services, PostgresPeopleOptions options)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(options, nameof(options));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(options.ConnectionString, nameof(options.ConnectionString));
        services.AddSingleton<IOptions<PostgresPeopleOptions>>(Options.Create(options));
        services.AddPostgresMigrations<PeopleDbContext, PostgresPeopleOptions>();
        services.AddDbContextFactory<PeopleDbContext>(dbOptions => dbOptions.UseNpgsql(
            options.ConnectionString, npgsqlOpts => npgsqlOpts.MigrationsHistoryTable("__EFMigrationsHistory", PostgresPeopleOptions.Schema)));

        return services;
    }
}