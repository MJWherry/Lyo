using Lyo.Exceptions;
using Lyo.FileMetadataStore.Postgres.Database;
using Lyo.FileStorage.Multipart;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.FileMetadataStore.Postgres;

/// <summary>
/// Builder for configuring PostgreSQL file metadata store with its dependencies. <see cref="Build" /> also registers
/// <see cref="PostgresMultipartUploadSessionStore" /> as <see cref="IMultipartUploadSessionStore" /> when none is already registered.
/// Usage examples: // Use configuration section:
/// services.AddPostgresFileMetadataStoreKeyed("postgres-metadata") .ConfigurePostgresFileStore("PostgresFileStore") .Build(); // Use action to configure:
/// services.AddPostgresFileMetadataStoreKeyed("postgres-metadata") .ConfigurePostgresFileStore(options => { options.ConnectionString = "Host=localhost;...";
/// options.EnableAutoMigrations = true; }) .Build();
/// </summary>
public sealed class PostgresFileMetadataStoreBuilder
{
    private readonly string _keyName;
    private readonly IServiceCollection _services;
    private string? _postgresFileStoreConfigSection;
    private Action<PostgresFileMetadataStoreOptions>? _postgresFileStoreConfigure;

    internal PostgresFileMetadataStoreBuilder(IServiceCollection services, string keyName)
    {
        _services = ArgumentHelpers.ThrowIfNullReturn(services, nameof(services));
        _keyName = ArgumentHelpers.ThrowIfNullReturn(keyName, nameof(keyName));
    }

    /// <summary>Configures PostgreSQL file store options using a configuration section name.</summary>
    /// <param name="configSectionName">The configuration section name (defaults to PostgresFileMetadataStoreOptions.SectionName)</param>
    /// <returns>The builder for chaining</returns>
    public PostgresFileMetadataStoreBuilder ConfigurePostgresFileStore(string configSectionName = PostgresFileMetadataStoreOptions.SectionName)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName, nameof(configSectionName));
        _postgresFileStoreConfigSection = configSectionName;
        return this;
    }

    /// <summary>Configures PostgreSQL file store options using an action.</summary>
    /// <param name="configure">Action to configure the options</param>
    /// <returns>The builder for chaining</returns>
    public PostgresFileMetadataStoreBuilder ConfigurePostgresFileStore(Action<PostgresFileMetadataStoreOptions> configure)
    {
        ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
        _postgresFileStoreConfigure = configure;
        return this;
    }

    /// <summary>Builds and registers the PostgreSQL file metadata store.</summary>
    /// <returns>The service collection for chaining</returns>
    public IServiceCollection Build()
    {
        // Configure PostgreSQL File Store Options
        var configSectionName = _postgresFileStoreConfigSection ?? PostgresFileMetadataStoreOptions.SectionName;
        if (!_services.Any(s => s.ServiceType == typeof(PostgresFileMetadataStoreOptions))) {
            if (_postgresFileStoreConfigure != null) {
                _services.AddSingleton<PostgresFileMetadataStoreOptions>(_ => {
                    var options = new PostgresFileMetadataStoreOptions();
                    _postgresFileStoreConfigure(options);
                    return options;
                });
            }
            else {
                _services.AddSingleton<PostgresFileMetadataStoreOptions>(provider => {
                    var configuration = provider.GetRequiredService<IConfiguration>();
                    var section = configuration.GetSection(configSectionName);
                    var options = new PostgresFileMetadataStoreOptions();
                    if (section.Exists())
                        section.Bind(options);

                    return options;
                });
            }
        }

        // Register FileMetadataStoreDbContextFactory if not already registered and configuration is provided
        if (!_services.Any(s => s.ServiceType == typeof(IDbContextFactory<FileMetadataStoreDbContext>))) {
            if (_postgresFileStoreConfigure == null && _postgresFileStoreConfigSection == null) {
                // No configuration provided - assume factory is already registered or will be registered separately
                // This allows the simple case: services.AddPostgresFileMetadataStoreKeyed("key").Build()
                // when the factory is already registered via AddFileMetadataStoreDbContextFactory
            }
            else {
                PostgresFileMetadataStoreOptions options;
                if (_postgresFileStoreConfigure != null) {
                    // Create options from action
                    options = new();
                    _postgresFileStoreConfigure(options);
                }
                else {
                    // Create options from config - we need to build a temporary service provider to get IConfiguration
                    // This is not ideal but necessary for the builder pattern
                    using var tempProvider = _services.BuildServiceProvider();
                    var configuration = tempProvider.GetRequiredService<IConfiguration>();
                    var section = configuration.GetSection(configSectionName);
                    options = new();
                    if (section.Exists())
                        section.Bind(options);
                }

                _services.AddFileMetadataStoreDbContextFactory(options);
            }
        }

        // Register the keyed metadata store
        if (!_services.Any(s => s.ServiceKey != null && s.ServiceKey.Equals(_keyName) && s.ServiceType == typeof(IFileMetadataStore))) {
            _services.AddKeyedScoped<PostgresFileMetadataStore>(
                _keyName, (provider, serviceKey) => {
                    var factory = provider.GetRequiredService<IDbContextFactory<FileMetadataStoreDbContext>>();
                    var dbContext = factory.CreateDbContext();
                    var loggerFactory = provider.GetService<ILoggerFactory>();
                    return new(dbContext, loggerFactory);
                });

            _services.AddKeyedScoped<IFileMetadataStore>(_keyName, (provider, serviceKey) => provider.GetRequiredKeyedService<PostgresFileMetadataStore>(_keyName));
        }

        if (!_services.Any(s => s.ServiceType == typeof(IMultipartUploadSessionStore)))
            _services.AddPostgresMultipartUploadSessionStore();

        return _services;
    }
}