using Lyo.EntityReference.Models;
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
    extension(IServiceCollection services)
    {
        /// <summary>Adds PostgreSQL note DbContextFactory to the service collection (IDbContextFactory only).</summary>
        public IServiceCollection AddNoteDbContextFactory(Action<PostgresNoteOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresNoteOptions();
            configure(options);
            return services.AddNoteDbContextFactory(options);
        }

        /// <summary>Adds PostgreSQL note DbContextFactory using configuration binding.</summary>
        public IServiceCollection AddNoteDbContextFactoryFromConfiguration(IConfiguration configuration, string configSectionName = PostgresNoteOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new PostgresNoteOptions();
            var section = configuration.GetSection(configSectionName);
            if (section.Exists())
                section.Bind(options);

            return services.AddNoteDbContextFactory(options);
        }

        /// <summary>Adds PostgreSQL note DbContextFactory to the service collection.</summary>
        public IServiceCollection AddNoteDbContextFactory(PostgresNoteOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(options.ConnectionString, nameof(options.ConnectionString));
            services.AddSingleton(Options.Create(options));
            services.AddPostgresMigrations<NoteDbContext, PostgresNoteOptions>();
            services.AddDbContextFactory<NoteDbContext>(dbOpts => dbOpts.UseNpgsql(
                options.ConnectionString, npgsqlOpts => npgsqlOpts.MigrationsHistoryTable("__EFMigrationsHistory", PostgresNoteOptions.Schema)));

            return services;
        }

        /// <summary>Adds PostgreSQL note DbContextFactory and PostgresNoteStore (INoteStore) to the service collection.</summary>
        public IServiceCollection AddPostgresNoteStore(Action<PostgresNoteOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresNoteOptions();
            configure(options);
            return services.AddPostgresNoteStore(options);
        }

        /// <summary>Adds PostgreSQL note store using configuration binding.</summary>
        public IServiceCollection AddPostgresNoteStoreFromConfiguration(IConfiguration configuration, string configSectionName = PostgresNoteOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new PostgresNoteOptions();
            var section = configuration.GetSection(configSectionName);
            if (section.Exists())
                section.Bind(options);

            return services.AddPostgresNoteStore(options);
        }

        /// <summary>Adds PostgreSQL note DbContextFactory and PostgresNoteStore to the service collection.</summary>
        public IServiceCollection AddPostgresNoteStore(PostgresNoteOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddNoteDbContextFactory(options);
            services.AddSingleton<INoteStore>(
                sp => new PostgresNoteStore(
                    sp.GetRequiredService<IDbContextFactory<NoteDbContext>>(),
                    sp.GetRequiredService<IOptions<EntityRefOptions>>(),
                    sp.GetServices<IEntityRefActionInterceptor>()));
            return services;
        }
    }
}