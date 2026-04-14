using Lyo.Exceptions;
using Lyo.Note.Postgres.Database;
using Lyo.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lyo.Note.Postgres;

/// <summary>Extension methods for PostgreSQL note store registration.</summary>
public static class Extensions
{
    /// <summary>Adds PostgreSQL note DbContextFactory to the service collection (IDbContextFactory only).</summary>
    public static IServiceCollection AddNoteDbContextFactory(this IServiceCollection services, Action<PostgresNoteOptions> configure)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
        var options = new PostgresNoteOptions();
        configure(options);
        return services.AddNoteDbContextFactory(options);
    }

    /// <summary>Adds PostgreSQL note DbContextFactory using configuration binding.</summary>
    public static IServiceCollection AddNoteDbContextFactoryFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = PostgresNoteOptions.SectionName)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configuration, nameof(configuration));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName, nameof(configSectionName));
        var options = new PostgresNoteOptions();
        var section = configuration.GetSection(configSectionName);
        if (section.Exists())
            section.Bind(options);

        return services.AddNoteDbContextFactory(options);
    }

    /// <summary>Adds PostgreSQL note DbContextFactory to the service collection.</summary>
    public static IServiceCollection AddNoteDbContextFactory(this IServiceCollection services, PostgresNoteOptions options)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(options, nameof(options));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(options.ConnectionString, nameof(options.ConnectionString));
        services.AddSingleton<IOptions<PostgresNoteOptions>>(Options.Create(options));
        services.AddPostgresMigrations<NoteDbContext, PostgresNoteOptions>();
        services.AddDbContextFactory<NoteDbContext>(dbOpts => dbOpts.UseNpgsql(
            options.ConnectionString, npgsqlOpts => npgsqlOpts.MigrationsHistoryTable("__EFMigrationsHistory", PostgresNoteOptions.Schema)));

        return services;
    }

    /// <summary>Adds PostgreSQL note DbContextFactory and PostgresNoteStore (INoteStore) to the service collection.</summary>
    public static IServiceCollection AddPostgresNoteStore(this IServiceCollection services, Action<PostgresNoteOptions> configure)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
        var options = new PostgresNoteOptions();
        configure(options);
        return services.AddPostgresNoteStore(options);
    }

    /// <summary>Adds PostgreSQL note store using configuration binding.</summary>
    public static IServiceCollection AddPostgresNoteStoreFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = PostgresNoteOptions.SectionName)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configuration, nameof(configuration));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName, nameof(configSectionName));
        var options = new PostgresNoteOptions();
        var section = configuration.GetSection(configSectionName);
        if (section.Exists())
            section.Bind(options);

        return services.AddPostgresNoteStore(options);
    }

    /// <summary>Adds PostgreSQL note DbContextFactory and PostgresNoteStore to the service collection.</summary>
    public static IServiceCollection AddPostgresNoteStore(this IServiceCollection services, PostgresNoteOptions options)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(options, nameof(options));
        services.AddNoteDbContextFactory(options);
        services.AddSingleton<INoteStore, PostgresNoteStore>();
        return services;
    }
}