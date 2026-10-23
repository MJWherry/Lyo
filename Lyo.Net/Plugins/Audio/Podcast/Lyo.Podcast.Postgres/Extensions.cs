using Lyo.Exceptions;
using Lyo.Podcast.Postgres.Database;
using Lyo.Postgres;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Podcast.Postgres;

/// <summary>DI helpers that register the PostgreSQL podcast store.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Registers <see cref="PodcastDbContext" /> from a configure callback.</summary>
        public IServiceCollection AddPodcastDbContextFactory(Action<PostgresPodcastOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresPodcastOptions();
            configure(options);
            return services.AddPodcastDbContextFactory(options);
        }

        /// <summary>Registers <see cref="PodcastDbContext" /> from configuration section <paramref name="configSectionName" />.</summary>
        public IServiceCollection AddPodcastDbContextFactoryFromConfiguration(IConfiguration configuration, string configSectionName = PostgresPodcastOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new PostgresPodcastOptions();
            configuration.GetSection(configSectionName).Bind(options);
            return services.AddPodcastDbContextFactory(options);
        }

        /// <summary>Registers <see cref="PodcastDbContext" /> from an options instance.</summary>
        public IServiceCollection AddPodcastDbContextFactory(PostgresPodcastOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddPostgresDbContextFactory<PodcastDbContext, PostgresPodcastOptions>(options);
            return services;
        }

        /// <summary>Registers the podcast database factory and <see cref="IPodcastStore" />.</summary>
        public IServiceCollection AddPostgresPodcastStore(Action<PostgresPodcastOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresPodcastOptions();
            configure(options);
            return services.AddPostgresPodcastStore(options);
        }

        /// <summary>Registers the podcast store from configuration section <paramref name="configSectionName" />.</summary>
        public IServiceCollection AddPostgresPodcastStoreFromConfiguration(IConfiguration configuration, string configSectionName = PostgresPodcastOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new PostgresPodcastOptions();
            configuration.GetSection(configSectionName).Bind(options);
            return services.AddPostgresPodcastStore(options);
        }

        /// <summary>Registers the podcast database factory and <see cref="IPodcastStore" /> from an options instance.</summary>
        public IServiceCollection AddPostgresPodcastStore(PostgresPodcastOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddPodcastDbContextFactory(options);
            services.AddSingleton<IPodcastStore, PostgresPodcastStore>();
            return services;
        }
    }
}