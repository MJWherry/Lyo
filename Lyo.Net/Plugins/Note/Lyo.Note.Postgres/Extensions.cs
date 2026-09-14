using Lyo.EntityReference.Models;
using Lyo.EntityReference.Postgres;
using Lyo.Exceptions;
using Lyo.Note.Postgres.Database;
using Lyo.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace Lyo.Note.Postgres;

/// <summary>DI helpers for PostgreSQL note store registration.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Registers PostgreSQL note DbContextFactory to the service collection (IDbContextFactory only).</summary>
        public IServiceCollection AddNoteDbContextFactory(Action<PostgresNoteOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresNoteOptions();
            configure(options);
            return services.AddNoteDbContextFactory(options);
        }

        /// <summary>Registers PostgreSQL note DbContextFactory using configuration binding.</summary>
        public IServiceCollection AddNoteDbContextFactoryFromConfiguration(IConfiguration configuration, string configSectionName = PostgresNoteOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new PostgresNoteOptions();
            configuration.GetSection(configSectionName).Bind(options);

            return services.AddNoteDbContextFactory(options);
        }

        /// <summary>Registers PostgreSQL note DbContextFactory to the service collection.</summary>
        public IServiceCollection AddNoteDbContextFactory(PostgresNoteOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddPostgresDbContextFactory<NoteDbContext, PostgresNoteOptions>(options);
            services.AddEntityRefOptions();

            return services;
        }

        /// <summary>Registers PostgreSQL note DbContextFactory and PostgresNoteStore (INoteStore) to the service collection.</summary>
        public IServiceCollection AddPostgresNoteStore(Action<PostgresNoteOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresNoteOptions();
            configure(options);
            return services.AddPostgresNoteStore(options);
        }

        /// <summary>Registers PostgreSQL note store using configuration binding.</summary>
        public IServiceCollection AddPostgresNoteStoreFromConfiguration(IConfiguration configuration, string configSectionName = PostgresNoteOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new PostgresNoteOptions();
            configuration.GetSection(configSectionName).Bind(options);

            return services.AddPostgresNoteStore(options);
        }

        /// <summary>Registers PostgreSQL note DbContextFactory and PostgresNoteStore to the service collection.</summary>
        public IServiceCollection AddPostgresNoteStore(PostgresNoteOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddNoteDbContextFactory(options);
            services.AddSingleton<INoteStore>(sp => new PostgresNoteStore(
                sp.GetRequiredService<IDbContextFactory<NoteDbContext>>(), sp.GetRequiredService<EntityRefOptions>(),
                sp.GetRequiredService<PostgresNoteOptions>(), sp.GetServices<IEntityRefActionInterceptor>()));

            return services;
        }
    }
}