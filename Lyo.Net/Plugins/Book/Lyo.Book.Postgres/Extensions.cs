using Lyo.Book.Postgres.Database;
using Lyo.Exceptions;
using Lyo.Postgres;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Book.Postgres;

/// <summary>DI helpers that register the PostgreSQL book store.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddBookDbContextFactory(Action<PostgresBookOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresBookOptions();
            configure(options);
            return services.AddBookDbContextFactory(options);
        }

        public IServiceCollection AddBookDbContextFactoryFromConfiguration(IConfiguration configuration, string configSectionName = PostgresBookOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new PostgresBookOptions();
            configuration.GetSection(configSectionName).Bind(options);
            return services.AddBookDbContextFactory(options);
        }

        public IServiceCollection AddBookDbContextFactory(PostgresBookOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddPostgresDbContextFactory<BookDbContext, PostgresBookOptions>(options);
            return services;
        }

        public IServiceCollection AddPostgresBookStore(Action<PostgresBookOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresBookOptions();
            configure(options);
            return services.AddPostgresBookStore(options);
        }

        public IServiceCollection AddPostgresBookStoreFromConfiguration(IConfiguration configuration, string configSectionName = PostgresBookOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new PostgresBookOptions();
            configuration.GetSection(configSectionName).Bind(options);
            return services.AddPostgresBookStore(options);
        }

        public IServiceCollection AddPostgresBookStore(PostgresBookOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddBookDbContextFactory(options);
            services.AddSingleton<IBookStore, PostgresBookStore>();
            return services;
        }
    }
}