using Lyo.Comic.Postgres.Database;
using Lyo.Exceptions;
using Lyo.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace Lyo.Comic.Postgres;

/// <summary>DI helpers that register the PostgreSQL comic store.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Registers the PostgreSQL comic DbContextFactory using explicit options.</summary>
        public IServiceCollection AddComicDbContextFactory(Action<PostgresComicOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresComicOptions();
            configure(options);
            return services.AddComicDbContextFactory(options);
        }

        /// <summary>Registers the PostgreSQL comic DbContextFactory by binding from configuration.</summary>
        public IServiceCollection AddComicDbContextFactoryFromConfiguration(IConfiguration configuration, string configSectionName = PostgresComicOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new PostgresComicOptions();
            configuration.GetSection(configSectionName).Bind(options);

            return services.AddComicDbContextFactory(options);
        }

        /// <summary>Registers the PostgreSQL comic DbContextFactory using a pre-built options instance.</summary>
        public IServiceCollection AddComicDbContextFactory(PostgresComicOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddPostgresDbContextFactory<ComicDbContext, PostgresComicOptions>(options);

            return services;
        }

        /// <summary>Registers the PostgreSQL comic DbContextFactory and <see cref="IComicStore" /> using explicit options.</summary>
        public IServiceCollection AddPostgresComicStore(Action<PostgresComicOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresComicOptions();
            configure(options);
            return services.AddPostgresComicStore(options);
        }

        /// <summary>Registers the PostgreSQL comic DbContextFactory and <see cref="IComicStore" /> by binding from configuration.</summary>
        public IServiceCollection AddPostgresComicStoreFromConfiguration(IConfiguration configuration, string configSectionName = PostgresComicOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new PostgresComicOptions();
            configuration.GetSection(configSectionName).Bind(options);

            return services.AddPostgresComicStore(options);
        }

        /// <summary>Registers the PostgreSQL comic DbContextFactory and <see cref="IComicStore" /> using a pre-built options instance.</summary>
        public IServiceCollection AddPostgresComicStore(PostgresComicOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddComicDbContextFactory(options);
            services.AddSingleton<IComicStore, PostgresComicStore>();
            return services;
        }
    }
}