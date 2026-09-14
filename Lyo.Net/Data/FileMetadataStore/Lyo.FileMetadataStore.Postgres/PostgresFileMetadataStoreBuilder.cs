using Lyo.Exceptions;
using Lyo.FileMetadataStore.Postgres.Database;
using Lyo.FileStorage.Multipart;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.FileMetadataStore.Postgres;

/// <summary>
/// Fluent setup for a keyed PostgreSQL file metadata store. <see cref="Build" /> also registers <see cref="PostgresMultipartUploadSessionStore" /> as
/// <see cref="IMultipartUploadSessionStore" /> when none is already registered. Bind from a section via <c>ConfigurePostgresFileStore("PostgresFileStore")</c>, or mutate
/// options in <c>ConfigurePostgresFileStore(options =&gt; …)</c>, then call <c>Build()</c>.
/// </summary>
public sealed class PostgresFileMetadataStoreBuilder
{
    private readonly string _keyName;
    private readonly IServiceCollection _services;
    private string? _postgresFileStoreConfigSection;
    private Action<PostgresFileMetadataStoreOptions>? _postgresFileStoreConfigure;

    internal PostgresFileMetadataStoreBuilder(IServiceCollection services, string keyName)
    {
        _services = ArgumentHelpers.ThrowIfNullReturn(services);
        _keyName = ArgumentHelpers.ThrowIfNullReturn(keyName);
    }

    /// <summary>Binds PostgreSQL file store options from a configuration section.</summary>
    /// <param name="configSectionName">Section name; starts as PostgresFileMetadataStoreOptions.SectionName.</param>
    /// <returns>This builder for chaining.</returns>
    public PostgresFileMetadataStoreBuilder ConfigurePostgresFileStore(string configSectionName = PostgresFileMetadataStoreOptions.SectionName)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
        _postgresFileStoreConfigSection = configSectionName;
        return this;
    }

    /// <summary>Mutates PostgreSQL file store options via a callback.</summary>
    /// <param name="configure">Callback that sets options.</param>
    /// <returns>This builder for chaining.</returns>
    public PostgresFileMetadataStoreBuilder ConfigurePostgresFileStore(Action<PostgresFileMetadataStoreOptions> configure)
    {
        ArgumentHelpers.ThrowIfNull(configure);
        _postgresFileStoreConfigure = configure;
        return this;
    }

    /// <summary>Registers the PostgreSQL file metadata store and returns the service collection.</summary>
    /// <returns>Service collection for chaining.</returns>
    public IServiceCollection Build()
    {
        // Bind or apply PostgreSQL file store options
        var configSectionName = _postgresFileStoreConfigSection ?? PostgresFileMetadataStoreOptions.SectionName;
        if (!_services.Any(s => s.ServiceType == typeof(PostgresFileMetadataStoreOptions))) {
            PostgresFileMetadataStoreOptions options;
            if (_postgresFileStoreConfigure != null) {
                options = new();
                _postgresFileStoreConfigure(options);
            }
            else {
                using var tempProvider = _services.BuildServiceProvider();
                options = new PostgresFileMetadataStoreOptions();
                tempProvider.GetRequiredService<IConfiguration>().GetSection(configSectionName).Bind(options);
            }

            options.Validate();
            _services.AddSingleton(options);
        }

        // Register FileMetadataStoreDbContextFactory when missing and options are available
        if (!_services.Any(s => s.ServiceType == typeof(IDbContextFactory<FileMetadataStoreDbContext>))) {
            if (_postgresFileStoreConfigure == null && _postgresFileStoreConfigSection == null) {
                // No options here — assume the factory is already registered, or will be via AddFileMetadataStoreDbContextFactory.
                // Allows services.AddPostgresFileMetadataStoreKeyed("key").Build() when the factory is already in DI.
            }
            else {
                PostgresFileMetadataStoreOptions options;
                if (_postgresFileStoreConfigure != null) {
                    options = new();
                    _postgresFileStoreConfigure(options);
                }
                else {
                    // The builder has no service provider of its own, so it stands one up to reach IConfiguration.
                    using var tempProvider = _services.BuildServiceProvider();
                    options = new PostgresFileMetadataStoreOptions();
                    tempProvider.GetRequiredService<IConfiguration>().GetSection(configSectionName).Bind(options);
                }

                _services.AddFileMetadataStoreDbContextFactory(options);
            }
        }

        // Register the keyed store when missing
        if (!_services.Any(s => s.ServiceKey != null && s.ServiceKey.Equals(_keyName) && s.ServiceType == typeof(IFileMetadataStore))) {
            _services.AddKeyedScoped<PostgresFileMetadataStore>(
                _keyName, (provider, _) => {
                    var factory = provider.GetRequiredService<IDbContextFactory<FileMetadataStoreDbContext>>();
                    var dbContext = factory.CreateDbContext();
                    var loggerFactory = provider.GetService<ILoggerFactory>();
                    return new(dbContext, loggerFactory);
                });

            _services.AddKeyedScoped<IFileMetadataStore>(_keyName, (provider, _) => provider.GetRequiredKeyedService<PostgresFileMetadataStore>(_keyName));
        }

        if (!_services.Any(s => s.ServiceType == typeof(IMultipartUploadSessionStore)))
            _services.AddPostgresMultipartUploadSessionStore();

        return _services;
    }
}