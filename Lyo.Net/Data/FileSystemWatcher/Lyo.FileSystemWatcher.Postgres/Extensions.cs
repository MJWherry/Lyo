using Lyo.Common.Core.Enums;
using Lyo.Exceptions;
using Lyo.FileSystemWatcher.Models;
using Lyo.FileSystemWatcher.Postgres.Database;
using Lyo.Hashing;
using Lyo.Hashing.Registration;
using Lyo.Postgres;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.FileSystemWatcher.Postgres;

/// <summary>DI helpers for the FileSystemWatcher PostgreSQL store.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Registers the watcher DbContext factory (IDbContextFactory only).</summary>
        public IServiceCollection AddFileSystemWatcherDbContextFactory(Action<PostgresFileSystemWatcherOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresFileSystemWatcherOptions();
            configure(options);
            return services.AddFileSystemWatcherDbContextFactory(options);
        }

        /// <summary>Registers the watcher DbContext factory from configuration.</summary>
        public IServiceCollection AddFileSystemWatcherDbContextFactoryFromConfiguration(
            IConfiguration configuration,
            string configSectionName = PostgresFileSystemWatcherOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new PostgresFileSystemWatcherOptions();
            configuration.GetSection(configSectionName).Bind(options);
            return services.AddFileSystemWatcherDbContextFactory(options);
        }

        /// <summary>Registers the watcher DbContext factory.</summary>
        public IServiceCollection AddFileSystemWatcherDbContextFactory(PostgresFileSystemWatcherOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddPostgresDbContextFactory<FileSystemWatcherDbContext, PostgresFileSystemWatcherOptions>(options);
            return services;
        }

        /// <summary>Registers the watcher store from a configure delegate.</summary>
        public IServiceCollection AddPostgresFileSystemWatcherStore(Action<PostgresFileSystemWatcherOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresFileSystemWatcherOptions();
            configure(options);
            return services.AddPostgresFileSystemWatcherStore(options);
        }

        /// <summary>Registers the watcher store from configuration.</summary>
        public IServiceCollection AddPostgresFileSystemWatcherStoreFromConfiguration(
            IConfiguration configuration,
            string configSectionName = PostgresFileSystemWatcherOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(configuration);
            var options = new PostgresFileSystemWatcherOptions();
            configuration.GetSection(configSectionName).Bind(options);
            return services.AddPostgresFileSystemWatcherStore(options);
        }

        /// <summary>Registers the watcher DbContext factory and <see cref="IFileSystemWatcherStore" />.</summary>
        public IServiceCollection AddPostgresFileSystemWatcherStore(PostgresFileSystemWatcherOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddFileSystemWatcherDbContextFactory(options);
            services.AddLyoHashing(o => o.DefaultHexLetterCase = TextLetterCase.Lower);
            services.TryAddSingleton<IFileSystemWatcherStore, PostgresFileSystemWatcherStore>();
            return services;
        }
    }
}
