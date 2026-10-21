using Lyo.Exceptions;
using Lyo.Postgres;
using Lyo.Video.Postgres.Database;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Video.Postgres;

/// <summary>DI helpers that register the PostgreSQL video store.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Registers <see cref="VideoDbContext" /> from a configure callback.</summary>
        public IServiceCollection AddVideoDbContextFactory(Action<PostgresVideoOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresVideoOptions();
            configure(options);
            return services.AddVideoDbContextFactory(options);
        }

        /// <summary>Registers <see cref="VideoDbContext" /> from configuration section <paramref name="configSectionName" />.</summary>
        public IServiceCollection AddVideoDbContextFactoryFromConfiguration(IConfiguration configuration, string configSectionName = PostgresVideoOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new PostgresVideoOptions();
            configuration.GetSection(configSectionName).Bind(options);
            return services.AddVideoDbContextFactory(options);
        }

        /// <summary>Registers <see cref="VideoDbContext" /> from an options instance.</summary>
        public IServiceCollection AddVideoDbContextFactory(PostgresVideoOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddPostgresDbContextFactory<VideoDbContext, PostgresVideoOptions>(options);
            return services;
        }

        /// <summary>Registers the video database factory and <see cref="IVideoStore" />.</summary>
        public IServiceCollection AddPostgresVideoStore(Action<PostgresVideoOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresVideoOptions();
            configure(options);
            return services.AddPostgresVideoStore(options);
        }

        /// <summary>Registers the video store from configuration section <paramref name="configSectionName" />.</summary>
        public IServiceCollection AddPostgresVideoStoreFromConfiguration(IConfiguration configuration, string configSectionName = PostgresVideoOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new PostgresVideoOptions();
            configuration.GetSection(configSectionName).Bind(options);
            return services.AddPostgresVideoStore(options);
        }

        /// <summary>Registers the video database factory and <see cref="IVideoStore" /> from an options instance.</summary>
        public IServiceCollection AddPostgresVideoStore(PostgresVideoOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddVideoDbContextFactory(options);
            services.AddSingleton<IVideoStore, PostgresVideoStore>();
            return services;
        }
    }
}