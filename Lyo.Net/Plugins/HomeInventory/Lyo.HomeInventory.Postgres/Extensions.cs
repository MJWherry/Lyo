using Lyo.Configuration;
using Lyo.EntityReference.Models;
using Lyo.EntityReference.Postgres;
using Lyo.Exceptions;
using Lyo.HomeInventory.Postgres.Database;
using Lyo.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace Lyo.HomeInventory.Postgres;

/// <summary>PostgreSQL home-inventory storage dI registration.</summary>
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
            var options = LyoOptions.Bind<PostgresHomeInventoryOptions>(configuration, configSectionName);

            return services.AddHomeInventoryDbContextFactory(options);
        }

        public IServiceCollection AddHomeInventoryDbContextFactory(PostgresHomeInventoryOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddPostgresDbContextFactory<HomeInventoryDbContext, PostgresHomeInventoryOptions>(options);
            services.AddEntityRefOptions();

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

        public IServiceCollection AddPostgresHomeInventoryStoreFromConfiguration(IConfiguration configuration, string configSectionName = PostgresHomeInventoryOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = LyoOptions.Bind<PostgresHomeInventoryOptions>(configuration, configSectionName);

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