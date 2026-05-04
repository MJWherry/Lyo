using Lyo.Exceptions;
using Lyo.HomeInventory.Postgres.Database;
using Lyo.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lyo.HomeInventory.Postgres;

/// <summary>DI registration for PostgreSQL home-inventory storage.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddHomeInventoryDbContextFactory(Action<PostgresHomeInventoryOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresHomeInventoryOptions();
            configure(options);
            return services.AddHomeInventoryDbContextFactory(options);
        }

        public IServiceCollection AddHomeInventoryDbContextFactoryFromConfiguration(
            IConfiguration configuration,
            string configSectionName = PostgresHomeInventoryOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new PostgresHomeInventoryOptions();
            var section = configuration.GetSection(configSectionName);
            if (section.Exists())
                section.Bind(options);

            return services.AddHomeInventoryDbContextFactory(options);
        }

        public IServiceCollection AddHomeInventoryDbContextFactory(PostgresHomeInventoryOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(options.ConnectionString, nameof(options.ConnectionString));
            services.AddSingleton<IOptions<PostgresHomeInventoryOptions>>(Options.Create(options));
            services.AddPostgresMigrations<HomeInventoryDbContext, PostgresHomeInventoryOptions>();
            services.AddDbContextFactory<HomeInventoryDbContext>(dbOpts => dbOpts.UseNpgsql(
                options.ConnectionString, npgsqlOpts => npgsqlOpts.MigrationsHistoryTable("__EFMigrationsHistory", PostgresHomeInventoryOptions.Schema)));

            return services;
        }

        public IServiceCollection AddPostgresHomeInventoryStore(Action<PostgresHomeInventoryOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresHomeInventoryOptions();
            configure(options);
            return services.AddPostgresHomeInventoryStore(options);
        }

        public IServiceCollection AddPostgresHomeInventoryStoreFromConfiguration(
            IConfiguration configuration,
            string configSectionName = PostgresHomeInventoryOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new PostgresHomeInventoryOptions();
            var section = configuration.GetSection(configSectionName);
            if (section.Exists())
                section.Bind(options);

            return services.AddPostgresHomeInventoryStore(options);
        }

        public IServiceCollection AddPostgresHomeInventoryStore(PostgresHomeInventoryOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddHomeInventoryDbContextFactory(options);
            services.AddSingleton<IHomeInventoryStore, PostgresHomeInventoryStore>();
            return services;
        }
    }
}