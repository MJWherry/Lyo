using Lyo.Exceptions;
using Lyo.FileMetadataStore.DownloadAccess;
using Lyo.FileMetadataStore.Postgres.Database;
using Lyo.FileStorage.Audit;
using Lyo.FileStorage.Multipart;
using Lyo.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
namespace Lyo.FileMetadataStore.Postgres;

/// <summary>DI helpers that register the PostgreSQL file metadata store and related DbContext factory.</summary>
public static class Extensions
{
    /// <param name="services">Service collection.</param>
    extension(IServiceCollection services)
    {
        /// <summary>Adds <see cref="FileMetadataStoreDbContext" /> using a connection string.</summary>
        /// <param name="connectionString">PostgreSQL connection string.</param>
        /// <returns>Service collection for chaining.</returns>
        public IServiceCollection AddFileMetadataStoreDbContext(string connectionString)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString);
            return services.AddFileMetadataStoreDbContextFactory(new PostgresFileMetadataStoreOptions { ConnectionString = connectionString })
                .AddScoped<FileMetadataStoreDbContext>(sp => sp.GetRequiredService<IDbContextFactory<FileMetadataStoreDbContext>>().CreateDbContext());
        }

        /// <summary>Adds the PostgreSQL file store DbContextFactory configured by the given options action.</summary>
        /// <param name="configure">Callback that mutates <see cref="PostgresFileMetadataStoreOptions" />.</param>
        /// <returns>Service collection for chaining.</returns>
        public IServiceCollection AddFileMetadataStoreDbContextFactory(Action<PostgresFileMetadataStoreOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresFileMetadataStoreOptions();
            configure(options);
            return services.AddFileMetadataStoreDbContextFactory(options);
        }

        /// <summary>Adds the PostgreSQL file store DbContextFactory from configuration (section <see cref="PostgresFileMetadataStoreOptions.SectionName" /> when omitted).</summary>
        /// <param name="configuration">Configuration root (e.g. builder.Configuration).</param>
        /// <param name="configSectionName">Section name; starts as PostgresFileMetadataStoreOptions.SectionName.</param>
        /// <returns>Service collection for chaining.</returns>
        public IServiceCollection AddFileMetadataStoreDbContextFactoryFromConfiguration(
            IConfiguration configuration,
            string configSectionName = PostgresFileMetadataStoreOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new PostgresFileMetadataStoreOptions();
            configuration.GetSection(configSectionName).Bind(options);

            return services.AddFileMetadataStoreDbContextFactory(options);
        }

        /// <summary>Adds the PostgreSQL file store DbContextFactory with the given options instance.</summary>
        /// <param name="options">PostgreSQL file store options.</param>
        /// <returns>Service collection for chaining.</returns>
        public IServiceCollection AddFileMetadataStoreDbContextFactory(PostgresFileMetadataStoreOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddPostgresDbContextFactory<FileMetadataStoreDbContext, PostgresFileMetadataStoreOptions>(options);

            return services;
        }

        /// <summary>Adds the PostgreSQL file metadata store using <see cref="FileMetadataStoreDbContext" /> from DI.</summary>
        /// <returns>Service collection for chaining.</returns>
        public IServiceCollection AddPostgresFileMetadataStore()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.AddScoped<PostgresFileMetadataStore>(provider => {
                var dbContext = provider.GetRequiredService<FileMetadataStoreDbContext>();
                var loggerFactory = provider.GetService<ILoggerFactory>();
                return new(dbContext, loggerFactory);
            });

            return services;
        }

        /// <summary>Adds the PostgreSQL file metadata store with a DbContext options callback.</summary>
        /// <param name="configure">Callback that configures the DbContext options builder.</param>
        /// <returns>Service collection for chaining.</returns>
        public IServiceCollection AddPostgresFileMetadataStore(Action<DbContextOptionsBuilder> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            services.AddDbContext<FileMetadataStoreDbContext>(configure);
            services.AddScoped<PostgresFileMetadataStore>(provider => {
                var dbContext = provider.GetRequiredService<FileMetadataStoreDbContext>();
                var loggerFactory = provider.GetService<ILoggerFactory>();
                return new(dbContext, loggerFactory);
            });

            return services;
        }

        /// <summary>Adds the PostgreSQL file metadata store using a connection string.</summary>
        /// <param name="connectionString">PostgreSQL connection string.</param>
        /// <returns>Service collection for chaining.</returns>
        public IServiceCollection AddPostgresFileMetadataStore(string connectionString)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString);
            services.AddFileMetadataStoreDbContextFactory(new PostgresFileMetadataStoreOptions { ConnectionString = connectionString });
            services.AddScoped<PostgresFileMetadataStore>(provider => {
                var factory = provider.GetRequiredService<IDbContextFactory<FileMetadataStoreDbContext>>();
                var dbContext = factory.CreateDbContext();
                var loggerFactory = provider.GetService<ILoggerFactory>();
                return new(dbContext, loggerFactory);
            });

            return services;
        }

        /// <summary>Adds a keyed PostgreSQL file metadata store with a DbContext options callback.</summary>
        /// <param name="keyName">DI key.</param>
        /// <param name="configure">Callback that configures the DbContext options builder.</param>
        /// <returns>Service collection for chaining.</returns>
        public IServiceCollection AddPostgresFileMetadataStoreKeyed(string keyName, Action<DbContextOptionsBuilder> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyName);
            ArgumentHelpers.ThrowIfNull(configure);
            services.AddDbContext<FileMetadataStoreDbContext>(configure);
            services.AddKeyedScoped<PostgresFileMetadataStore>(
                keyName, (provider, _) => {
                    var dbContext = provider.GetRequiredService<FileMetadataStoreDbContext>();
                    var loggerFactory = provider.GetService<ILoggerFactory>();
                    return new(dbContext, loggerFactory);
                });

            services.AddKeyedScoped<IFileMetadataStore>(keyName, (provider, _) => provider.GetRequiredKeyedService<PostgresFileMetadataStore>(keyName));
            return services;
        }

        /// <summary>Adds a keyed PostgreSQL file metadata store using a connection string.</summary>
        /// <param name="keyName">DI key.</param>
        /// <param name="connectionString">PostgreSQL connection string.</param>
        /// <returns>Service collection for chaining.</returns>
        public IServiceCollection AddPostgresFileMetadataStoreKeyed(string keyName, string connectionString)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyName);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString);
            services.AddFileMetadataStoreDbContextFactory(new PostgresFileMetadataStoreOptions { ConnectionString = connectionString });
            services.AddKeyedScoped<PostgresFileMetadataStore>(
                keyName, (provider, _) => {
                    var factory = provider.GetRequiredService<IDbContextFactory<FileMetadataStoreDbContext>>();
                    var dbContext = factory.CreateDbContext();
                    var loggerFactory = provider.GetService<ILoggerFactory>();
                    return new(dbContext, loggerFactory);
                });

            services.AddKeyedScoped<IFileMetadataStore>(keyName, (provider, _) => provider.GetRequiredKeyedService<PostgresFileMetadataStore>(keyName));
            return services;
        }

        /// <summary>Adds a keyed PostgreSQL file metadata store and returns a builder for further setup.</summary>
        /// <param name="keyName">DI key.</param>
        /// <returns>Builder for configuring the store and its dependencies.</returns>
        /// <example>
        /// <code>
        /// // Use configuration section:
        /// services.AddPostgresFileMetadataStoreKeyed("postgres-metadata")
        ///     .ConfigurePostgresFileStore("PostgresFileStore")
        ///     .Build();
        /// 
        /// // Use action to configure:
        /// services.AddPostgresFileMetadataStoreKeyed("postgres-metadata")
        ///     .ConfigurePostgresFileStore(options => {
        ///         options.ConnectionString = "Host=localhost;...";
        ///         options.EnableAutoMigrations = true;
        ///     })
        ///     .Build();
        /// </code>
        /// </example>
        public PostgresFileMetadataStoreBuilder AddPostgresFileMetadataStoreKeyed(string keyName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyName);
            return new(services, keyName);
        }

        /// <summary>Registers <see cref="PostgresFileAuditSink" /> as <see cref="IFileAuditEventHandler" />.</summary>
        public IServiceCollection AddPostgresFileAuditSink()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.AddScoped<IFileAuditEventHandler, PostgresFileAuditSink>();
            return services;
        }

        /// <summary>
        /// Registers <see cref="PostgresMultipartUploadSessionStore" /> as <see cref="IMultipartUploadSessionStore" />. Typically invoked by
        /// <see cref="PostgresFileMetadataStoreBuilder.Build" /> when no session store is registered yet.
        /// </summary>
        public IServiceCollection AddPostgresMultipartUploadSessionStore()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.AddScoped<PostgresMultipartUploadSessionStore>();
            services.AddScoped<IMultipartUploadSessionStore>(sp => sp.GetRequiredService<PostgresMultipartUploadSessionStore>());
            return services;
        }

        /// <summary>Registers <see cref="PostgresFileDownloadAccessService" /> as <see cref="IFileDownloadAccessService" />.</summary>
        public IServiceCollection AddPostgresFileDownloadAccessService()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.AddScoped<PostgresFileDownloadAccessService>();
            services.AddScoped<IFileDownloadAccessService>(sp => sp.GetRequiredService<PostgresFileDownloadAccessService>());
            return services;
        }
    }
}