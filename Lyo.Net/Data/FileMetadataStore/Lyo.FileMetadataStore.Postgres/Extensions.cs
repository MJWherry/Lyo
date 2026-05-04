using Lyo.Exceptions;
using Lyo.FileMetadataStore.Postgres.Database;
using Lyo.FileStorage.Audit;
using Lyo.FileStorage.Multipart;
using Lyo.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lyo.FileMetadataStore.Postgres;

/// <summary>Extension methods for PostgreSQL file store database context registration.</summary>
public static class Extensions
{
    /// <param name="services">The service collection</param>
    extension(IServiceCollection services)
    {
        /// <summary>Adds FileMetadataStoreDbContext to the service collection.</summary>
        /// <param name="connectionString">The PostgreSQL connection string</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddFileMetadataStoreDbContext(string connectionString)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString);
            return services.AddFileMetadataStoreDbContextFactory(new PostgresFileMetadataStoreOptions { ConnectionString = connectionString })
                .AddScoped<FileMetadataStoreDbContext>(sp => sp.GetRequiredService<IDbContextFactory<FileMetadataStoreDbContext>>().CreateDbContext());
        }

        /// <summary>Adds FileMetadataStoreDbContext to the service collection.</summary>
        /// <param name="configure">Action to configure the DbContext options</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddFileMetadataStoreDbContext(Action<DbContextOptionsBuilder> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            services.AddDbContext<FileMetadataStoreDbContext>(configure);
            return services;
        }

        /// <summary>Adds PostgreSQL file store DbContextFactory to the service collection.</summary>
        /// <param name="configure">Action to configure the PostgreSQL file store options</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddFileMetadataStoreDbContextFactory(Action<PostgresFileMetadataStoreOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresFileMetadataStoreOptions();
            configure(options);
            return services.AddFileMetadataStoreDbContextFactory(options);
        }

        /// <summary>Adds PostgreSQL file store DbContextFactory to the service collection using configuration binding.</summary>
        /// <param name="configuration">The configuration (e.g. builder.Configuration)</param>
        /// <param name="configSectionName">The configuration section name (defaults to PostgresFileMetadataStoreOptions.SectionName)</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddFileMetadataStoreDbContextFactoryFromConfiguration(
            IConfiguration configuration,
            string configSectionName = PostgresFileMetadataStoreOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new PostgresFileMetadataStoreOptions();
            var section = configuration.GetSection(configSectionName);
            if (section.Exists())
                section.Bind(options);

            return services.AddFileMetadataStoreDbContextFactory(options);
        }

        /// <summary>Adds PostgreSQL file store DbContextFactory to the service collection.</summary>
        /// <param name="options">The PostgreSQL file store options</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddFileMetadataStoreDbContextFactory(PostgresFileMetadataStoreOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(options.ConnectionString, nameof(options.ConnectionString));
            services.AddSingleton(Options.Create(options));
            services.AddPostgresMigrations<FileMetadataStoreDbContext, PostgresFileMetadataStoreOptions>();
            services.AddDbContextFactory<FileMetadataStoreDbContext>(dbOptions => dbOptions.UseNpgsql(
                options.ConnectionString, npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", PostgresFileMetadataStoreOptions.Schema)));

            return services;
        }

        /// <summary>Adds PostgreSQL file metadata store to the service collection using FileMetadataStoreDbContext from DI.</summary>
        /// <returns>The service collection for chaining</returns>
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

        /// <summary>Adds PostgreSQL file metadata store to the service collection with configuration.</summary>
        /// <param name="configure">Action to configure the DbContext options builder</param>
        /// <returns>The service collection for chaining</returns>
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

        /// <summary>Adds PostgreSQL file metadata store to the service collection with connection string.</summary>
        /// <param name="connectionString">The PostgreSQL connection string</param>
        /// <returns>The service collection for chaining</returns>
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

        /// <summary>Adds a keyed PostgreSQL file metadata store to the service collection with configuration.</summary>
        /// <param name="keyName">The key name for the keyed metadata store service</param>
        /// <param name="configure">Action to configure the DbContext options builder</param>
        /// <returns>The service collection for chaining</returns>
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

        /// <summary>Adds a keyed PostgreSQL file metadata store to the service collection with connection string.</summary>
        /// <param name="keyName">The key name for the keyed metadata store service</param>
        /// <param name="connectionString">The PostgreSQL connection string</param>
        /// <returns>The service collection for chaining</returns>
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

        /// <summary>Adds a keyed PostgreSQL file metadata store to the service collection using a builder pattern.</summary>
        /// <param name="keyName">The key name for the keyed metadata store service</param>
        /// <returns>A builder for configuring the service and its dependencies</returns>
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
        /// Registers <see cref="PostgresMultipartUploadSessionStore" /> as <see cref="IMultipartUploadSessionStore" />. Usually invoked automatically by
        /// <see cref="PostgresFileMetadataStoreBuilder.Build" /> when no session store is registered yet.
        /// </summary>
        public IServiceCollection AddPostgresMultipartUploadSessionStore()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.AddScoped<PostgresMultipartUploadSessionStore>();
            services.AddScoped<IMultipartUploadSessionStore>(sp => sp.GetRequiredService<PostgresMultipartUploadSessionStore>());
            return services;
        }
    }
}