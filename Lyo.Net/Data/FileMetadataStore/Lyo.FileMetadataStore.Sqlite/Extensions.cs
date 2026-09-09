using Lyo.Configuration;
using Lyo.Exceptions;
using Lyo.FileMetadataStore.DownloadAccess;
using Lyo.FileMetadataStore.Sqlite.Database;
using Lyo.FileStorage.Audit;
using Lyo.FileStorage.Multipart;
using Lyo.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lyo.FileMetadataStore.Sqlite;

/// <summary>DI helpers that register the SQLite file metadata store and related DbContext factory.</summary>
public static class Extensions
{
    /// <param name="services">Service collection.</param>
    extension(IServiceCollection services)
    {
        /// <summary>Adds <see cref="SqliteFileMetadataStoreDbContext" /> using a connection string.</summary>
        /// <param name="connectionString">SQLite connection string.</param>
        /// <returns>Service collection for chaining.</returns>
        public IServiceCollection AddSqliteFileMetadataStoreDbContext(string connectionString)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString);
            return services.AddSqliteFileMetadataStoreDbContextFactory(new SqliteFileMetadataStoreOptions { ConnectionString = connectionString })
                .AddScoped<SqliteFileMetadataStoreDbContext>(sp => sp.GetRequiredService<IDbContextFactory<SqliteFileMetadataStoreDbContext>>().CreateDbContext());
        }

        /// <summary>Adds <see cref="SqliteFileMetadataStoreDbContext" /> with a DbContext options callback.</summary>
        /// <param name="configure">Callback that configures the DbContext options.</param>
        /// <returns>Service collection for chaining.</returns>
        public IServiceCollection AddSqliteFileMetadataStoreDbContext(Action<DbContextOptionsBuilder> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            services.AddDbContext<SqliteFileMetadataStoreDbContext>(configure);
            return services;
        }

        /// <summary>Adds the SQLite file store DbContextFactory configured by the given options action.</summary>
        /// <param name="configure">Callback that mutates <see cref="SqliteFileMetadataStoreOptions" />.</param>
        /// <returns>Service collection for chaining.</returns>
        public IServiceCollection AddSqliteFileMetadataStoreDbContextFactory(Action<SqliteFileMetadataStoreOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new SqliteFileMetadataStoreOptions();
            configure(options);
            return services.AddSqliteFileMetadataStoreDbContextFactory(options);
        }

        /// <summary>Adds the SQLite file store DbContextFactory from configuration (section <see cref="SqliteFileMetadataStoreOptions.SectionName" /> when omitted).</summary>
        /// <param name="configuration">Configuration root (e.g. builder.Configuration).</param>
        /// <param name="configSectionName">Section name; starts as <see cref="SqliteFileMetadataStoreOptions.SectionName" />.</param>
        /// <returns>Service collection for chaining.</returns>
        public IServiceCollection AddSqliteFileMetadataStoreDbContextFactoryFromConfiguration(
            IConfiguration configuration,
            string configSectionName = SqliteFileMetadataStoreOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = LyoOptions.Bind<SqliteFileMetadataStoreOptions>(configuration, configSectionName);

            return services.AddSqliteFileMetadataStoreDbContextFactory(options);
        }

        /// <summary>Adds the SQLite file store DbContextFactory with the given options instance.</summary>
        /// <param name="options">SQLite file store options.</param>
        /// <returns>Service collection for chaining.</returns>
        public IServiceCollection AddSqliteFileMetadataStoreDbContextFactory(SqliteFileMetadataStoreOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(options.ConnectionString, nameof(options.ConnectionString));
            services.AddSingleton(Options.Create(options));
            services.TryAddSingleton(options);
            services.AddSqliteMigrations<SqliteFileMetadataStoreDbContext, SqliteFileMetadataStoreOptions>();
            services.AddDbContextFactory<SqliteFileMetadataStoreDbContext>(dbOptions => dbOptions.UseSqlite(options.ConnectionString));
            return services;
        }

        /// <summary>Adds the SQLite file metadata store using <see cref="SqliteFileMetadataStoreDbContext" /> from DI.</summary>
        /// <returns>Service collection for chaining.</returns>
        public IServiceCollection AddSqliteFileMetadataStore()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.AddScoped<SqliteFileMetadataStore>(provider => {
                var dbContext = provider.GetRequiredService<SqliteFileMetadataStoreDbContext>();
                var loggerFactory = provider.GetService<ILoggerFactory>();
                return new(dbContext, loggerFactory);
            });

            return services;
        }

        /// <summary>Adds the SQLite file metadata store with a DbContext options callback.</summary>
        /// <param name="configure">Callback that configures the DbContext options builder.</param>
        /// <returns>Service collection for chaining.</returns>
        public IServiceCollection AddSqliteFileMetadataStore(Action<DbContextOptionsBuilder> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            services.AddDbContext<SqliteFileMetadataStoreDbContext>(configure);
            services.AddScoped<SqliteFileMetadataStore>(provider => {
                var dbContext = provider.GetRequiredService<SqliteFileMetadataStoreDbContext>();
                var loggerFactory = provider.GetService<ILoggerFactory>();
                return new(dbContext, loggerFactory);
            });

            return services;
        }

        /// <summary>Adds the SQLite file metadata store using a connection string.</summary>
        /// <param name="connectionString">SQLite connection string.</param>
        /// <returns>Service collection for chaining.</returns>
        public IServiceCollection AddSqliteFileMetadataStore(string connectionString)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString);
            services.AddSqliteFileMetadataStoreDbContextFactory(new SqliteFileMetadataStoreOptions { ConnectionString = connectionString });
            services.AddScoped<SqliteFileMetadataStore>(provider => {
                var factory = provider.GetRequiredService<IDbContextFactory<SqliteFileMetadataStoreDbContext>>();
                var dbContext = factory.CreateDbContext();
                var loggerFactory = provider.GetService<ILoggerFactory>();
                return new(dbContext, loggerFactory);
            });

            return services;
        }

        /// <summary>Adds a keyed SQLite file metadata store with a DbContext options callback.</summary>
        /// <param name="keyName">DI key.</param>
        /// <param name="configure">Callback that configures the DbContext options builder.</param>
        /// <returns>Service collection for chaining.</returns>
        public IServiceCollection AddSqliteFileMetadataStoreKeyed(string keyName, Action<DbContextOptionsBuilder> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyName);
            ArgumentHelpers.ThrowIfNull(configure);
            services.AddDbContext<SqliteFileMetadataStoreDbContext>(configure);
            services.AddKeyedScoped<SqliteFileMetadataStore>(
                keyName, (provider, _) => {
                    var dbContext = provider.GetRequiredService<SqliteFileMetadataStoreDbContext>();
                    var loggerFactory = provider.GetService<ILoggerFactory>();
                    return new(dbContext, loggerFactory);
                });

            services.AddKeyedScoped<IFileMetadataStore>(keyName, (provider, _) => provider.GetRequiredKeyedService<SqliteFileMetadataStore>(keyName));
            return services;
        }

        /// <summary>Adds a keyed SQLite file metadata store using a connection string.</summary>
        /// <param name="keyName">DI key.</param>
        /// <param name="connectionString">SQLite connection string.</param>
        /// <returns>Service collection for chaining.</returns>
        public IServiceCollection AddSqliteFileMetadataStoreKeyed(string keyName, string connectionString)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyName);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString);
            services.AddSqliteFileMetadataStoreDbContextFactory(new SqliteFileMetadataStoreOptions { ConnectionString = connectionString });
            services.AddKeyedScoped<SqliteFileMetadataStore>(
                keyName, (provider, _) => {
                    var factory = provider.GetRequiredService<IDbContextFactory<SqliteFileMetadataStoreDbContext>>();
                    var dbContext = factory.CreateDbContext();
                    var loggerFactory = provider.GetService<ILoggerFactory>();
                    return new(dbContext, loggerFactory);
                });

            services.AddKeyedScoped<IFileMetadataStore>(keyName, (provider, _) => provider.GetRequiredKeyedService<SqliteFileMetadataStore>(keyName));
            return services;
        }

        /// <summary>Adds a keyed SQLite file metadata store and returns a builder for further setup.</summary>
        /// <param name="keyName">DI key.</param>
        /// <returns>Builder for configuring the store and its dependencies.</returns>
        public SqliteFileMetadataStoreBuilder AddSqliteFileMetadataStoreKeyed(string keyName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyName);
            return new(services, keyName);
        }

        /// <summary>Registers <see cref="SqliteFileAuditSink" /> as <see cref="IFileAuditEventHandler" />.</summary>
        public IServiceCollection AddSqliteFileAuditSink()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.AddScoped<IFileAuditEventHandler, SqliteFileAuditSink>();
            return services;
        }

        /// <summary>
        /// Registers <see cref="SqliteMultipartUploadSessionStore" /> as <see cref="IMultipartUploadSessionStore" />. Typically invoked by
        /// <see cref="SqliteFileMetadataStoreBuilder.Build" /> when no session store is registered yet.
        /// </summary>
        public IServiceCollection AddSqliteMultipartUploadSessionStore()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.AddScoped<SqliteMultipartUploadSessionStore>();
            services.AddScoped<IMultipartUploadSessionStore>(sp => sp.GetRequiredService<SqliteMultipartUploadSessionStore>());
            return services;
        }

        /// <summary>Registers <see cref="SqliteFileDownloadAccessService" /> as <see cref="IFileDownloadAccessService" />.</summary>
        public IServiceCollection AddSqliteFileDownloadAccessService()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.AddScoped<SqliteFileDownloadAccessService>();
            services.AddScoped<IFileDownloadAccessService>(sp => sp.GetRequiredService<SqliteFileDownloadAccessService>());
            return services;
        }
    }
}