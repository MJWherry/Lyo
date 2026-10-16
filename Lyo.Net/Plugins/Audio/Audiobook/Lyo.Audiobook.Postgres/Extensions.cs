using Lyo.Audiobook.Postgres.Database;
using Lyo.Exceptions;
using Lyo.Postgres;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Audiobook.Postgres;

/// <summary>DI helpers that register the PostgreSQL audiobook store.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Registers <see cref="AudiobookDbContext" /> from a configure callback.</summary>
        public IServiceCollection AddAudiobookDbContextFactory(Action<PostgresAudiobookOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresAudiobookOptions();
            configure(options);
            return services.AddAudiobookDbContextFactory(options);
        }

        /// <summary>Registers <see cref="AudiobookDbContext" /> from configuration section <paramref name="configSectionName" />.</summary>
        public IServiceCollection AddAudiobookDbContextFactoryFromConfiguration(IConfiguration configuration, string configSectionName = PostgresAudiobookOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new PostgresAudiobookOptions();
            configuration.GetSection(configSectionName).Bind(options);
            return services.AddAudiobookDbContextFactory(options);
        }

        /// <summary>Registers <see cref="AudiobookDbContext" /> from an options instance.</summary>
        public IServiceCollection AddAudiobookDbContextFactory(PostgresAudiobookOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddPostgresDbContextFactory<AudiobookDbContext, PostgresAudiobookOptions>(options);
            return services;
        }

        /// <summary>Registers the audiobook database factory and <see cref="IAudiobookStore" />.</summary>
        public IServiceCollection AddPostgresAudiobookStore(Action<PostgresAudiobookOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresAudiobookOptions();
            configure(options);
            return services.AddPostgresAudiobookStore(options);
        }

        /// <summary>Registers the audiobook store from configuration section <paramref name="configSectionName" />.</summary>
        public IServiceCollection AddPostgresAudiobookStoreFromConfiguration(IConfiguration configuration, string configSectionName = PostgresAudiobookOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new PostgresAudiobookOptions();
            configuration.GetSection(configSectionName).Bind(options);
            return services.AddPostgresAudiobookStore(options);
        }

        /// <summary>Registers the audiobook database factory and <see cref="IAudiobookStore" /> from an options instance.</summary>
        public IServiceCollection AddPostgresAudiobookStore(PostgresAudiobookOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddAudiobookDbContextFactory(options);
            services.AddSingleton<IAudiobookStore, PostgresAudiobookStore>();
            return services;
        }
    }
}
