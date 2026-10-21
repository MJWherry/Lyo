using Lyo.Algorithms.Postgres.Database;
using Lyo.Exceptions;
using Lyo.Health;
using Lyo.Postgres;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Algorithms.Postgres;

/// <summary>DI helpers for the PostgreSQL algorithm catalog.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Registers <see cref="AlgorithmDbContext" /> from <paramref name="configure" />.</summary>
        public IServiceCollection AddAlgorithmDbContextFactory(Action<PostgresAlgorithmOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresAlgorithmOptions();
            configure(options);
            return services.AddAlgorithmDbContextFactory(options);
        }

        /// <summary>Registers <see cref="AlgorithmDbContext" /> from configuration section <see cref="PostgresAlgorithmOptions.SectionName" />.</summary>
        public IServiceCollection AddAlgorithmDbContextFactoryFromConfiguration(IConfiguration configuration, string sectionName = PostgresAlgorithmOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(sectionName);
            var options = new PostgresAlgorithmOptions();
            configuration.GetSection(sectionName).Bind(options);
            return services.AddAlgorithmDbContextFactory(options);
        }

        /// <summary>Registers <see cref="AlgorithmDbContext" /> from an options instance.</summary>
        public IServiceCollection AddAlgorithmDbContextFactory(PostgresAlgorithmOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddPostgresDbContextFactory<AlgorithmDbContext, PostgresAlgorithmOptions>(options);
            return services;
        }

        /// <summary>Registers <see cref="IAlgorithmDefinitionStore" /> as <see cref="PostgresAlgorithmDefinitionStore" />.</summary>
        public IServiceCollection AddPostgresAlgorithmStore(Action<PostgresAlgorithmOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresAlgorithmOptions();
            configure(options);
            return services.AddPostgresAlgorithmStore(options);
        }

        /// <summary>Registers the Postgres catalog from configuration.</summary>
        public IServiceCollection AddPostgresAlgorithmStoreFromConfiguration(IConfiguration configuration, string sectionName = PostgresAlgorithmOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(sectionName);
            var options = new PostgresAlgorithmOptions();
            configuration.GetSection(sectionName).Bind(options);
            return services.AddPostgresAlgorithmStore(options);
        }

        /// <summary>Registers the Postgres catalog from an options instance. Also registers <see cref="Lyo.Health.IHealth" />.</summary>
        public IServiceCollection AddPostgresAlgorithmStore(PostgresAlgorithmOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddAlgorithmDbContextFactory(options);
            services.AddSingleton<PostgresAlgorithmDefinitionStore>();
            services.AddSingleton<IAlgorithmDefinitionStore>(sp => sp.GetRequiredService<PostgresAlgorithmDefinitionStore>());
            services.AddSingleton<IHealth>(sp => sp.GetRequiredService<PostgresAlgorithmDefinitionStore>());
            return services;
        }
    }
}