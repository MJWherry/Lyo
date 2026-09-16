using Lyo.Diagram.Models;
using Lyo.Diagram.Postgres.Database;
using Lyo.Exceptions;
using Lyo.Postgres;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.Diagram.Postgres;

/// <summary>Helpers that register PostgreSQL diagram storage.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Registers a <see cref="DiagramDbContext" /> factory from a configure delegate.</summary>
        public IServiceCollection AddDiagramDbContextFactory(Action<PostgresDiagramOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresDiagramOptions();
            configure(options);
            return services.AddDiagramDbContextFactory(options);
        }

        /// <summary>Registers a <see cref="DiagramDbContext" /> factory from configuration.</summary>
        public IServiceCollection AddDiagramDbContextFactoryFromConfiguration(IConfiguration configuration, string configSectionName = PostgresDiagramOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new PostgresDiagramOptions();
            configuration.GetSection(configSectionName).Bind(options);
            return services.AddDiagramDbContextFactory(options);
        }

        /// <summary>Registers a <see cref="DiagramDbContext" /> factory from options.</summary>
        public IServiceCollection AddDiagramDbContextFactory(PostgresDiagramOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddPostgresDbContextFactory<DiagramDbContext, PostgresDiagramOptions>(options);
            return services;
        }

        /// <summary>Registers the PostgreSQL diagram store from a configure delegate.</summary>
        public IServiceCollection AddPostgresDiagramStore(Action<PostgresDiagramOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresDiagramOptions();
            configure(options);
            return services.AddPostgresDiagramStore(options);
        }

        /// <summary>Registers the PostgreSQL diagram store from configuration. Missing sections keep the declared defaults.</summary>
        public IServiceCollection AddPostgresDiagramStoreFromConfiguration(IConfiguration configuration, string configSectionName = PostgresDiagramOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new PostgresDiagramOptions();
            configuration.GetSection(configSectionName).Bind(options);
            return services.AddPostgresDiagramStore(options);
        }

        /// <summary>Registers the PostgreSQL diagram store from options.</summary>
        public IServiceCollection AddPostgresDiagramStore(PostgresDiagramOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddDiagramDbContextFactory(options);
            services.TryAddSingleton<IDiagramStore, PostgresDiagramStore>();
            return services;
        }
    }
}
