using Lyo.Exceptions;
using Lyo.Music.Postgres.Database;
using Lyo.Postgres;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Music.Postgres;

/// <summary>DI helpers that register the PostgreSQL music store.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Registers <see cref="MusicDbContext" /> from a configure callback.</summary>
        public IServiceCollection AddMusicDbContextFactory(Action<PostgresMusicOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresMusicOptions();
            configure(options);
            return services.AddMusicDbContextFactory(options);
        }

        /// <summary>Registers <see cref="MusicDbContext" /> from configuration section <paramref name="configSectionName" />.</summary>
        public IServiceCollection AddMusicDbContextFactoryFromConfiguration(IConfiguration configuration, string configSectionName = PostgresMusicOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new PostgresMusicOptions();
            configuration.GetSection(configSectionName).Bind(options);
            return services.AddMusicDbContextFactory(options);
        }

        /// <summary>Registers <see cref="MusicDbContext" /> from an options instance. Calls <c>Validate()</c> via the Postgres factory.</summary>
        public IServiceCollection AddMusicDbContextFactory(PostgresMusicOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddPostgresDbContextFactory<MusicDbContext, PostgresMusicOptions>(options);
            return services;
        }

        /// <summary>Registers the music database factory and <see cref="IMusicStore" />.</summary>
        public IServiceCollection AddPostgresMusicStore(Action<PostgresMusicOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresMusicOptions();
            configure(options);
            return services.AddPostgresMusicStore(options);
        }

        /// <summary>Registers the music store from configuration section <paramref name="configSectionName" />.</summary>
        public IServiceCollection AddPostgresMusicStoreFromConfiguration(IConfiguration configuration, string configSectionName = PostgresMusicOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new PostgresMusicOptions();
            configuration.GetSection(configSectionName).Bind(options);
            return services.AddPostgresMusicStore(options);
        }

        /// <summary>Registers the music database factory and <see cref="IMusicStore" /> from an options instance.</summary>
        public IServiceCollection AddPostgresMusicStore(PostgresMusicOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddMusicDbContextFactory(options);
            services.AddSingleton<IMusicStore, PostgresMusicStore>();
            return services;
        }
    }
}
