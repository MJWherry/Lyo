using Lyo.Configuration;
using Lyo.Exceptions;
using Lyo.FileMetadataStore.Sqlite.Database;
using Lyo.FileStorage.Multipart;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.FileMetadataStore.Sqlite;

/// <summary>Fluent setup for a keyed SQLite file metadata store and its dependencies.</summary>
public sealed class SqliteFileMetadataStoreBuilder
{
    private readonly string _keyName;
    private readonly IServiceCollection _services;
    private string? _sqliteFileStoreConfigSection;
    private Action<SqliteFileMetadataStoreOptions>? _sqliteFileStoreConfigure;

    internal SqliteFileMetadataStoreBuilder(IServiceCollection services, string keyName)
    {
        _services = ArgumentHelpers.ThrowIfNullReturn(services);
        _keyName = ArgumentHelpers.ThrowIfNullReturn(keyName);
    }

    /// <summary>Binds SQLite file store options from a configuration section.</summary>
    /// <param name="configSectionName">Section name; starts as <see cref="SqliteFileMetadataStoreOptions.SectionName" />.</param>
    /// <returns>This builder for chaining.</returns>
    public SqliteFileMetadataStoreBuilder ConfigureSqliteFileStore(string configSectionName = SqliteFileMetadataStoreOptions.SectionName)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
        _sqliteFileStoreConfigSection = configSectionName;
        return this;
    }

    /// <summary>Mutates SQLite file store options via a callback.</summary>
    /// <param name="configure">Callback that sets options.</param>
    /// <returns>This builder for chaining.</returns>
    public SqliteFileMetadataStoreBuilder ConfigureSqliteFileStore(Action<SqliteFileMetadataStoreOptions> configure)
    {
        ArgumentHelpers.ThrowIfNull(configure);
        _sqliteFileStoreConfigure = configure;
        return this;
    }

    /// <summary>Registers the SQLite file metadata store and returns the service collection.</summary>
    /// <returns>Service collection for chaining.</returns>
    public IServiceCollection Build()
    {
        var configSectionName = _sqliteFileStoreConfigSection ?? SqliteFileMetadataStoreOptions.SectionName;
        if (!_services.Any(s => s.ServiceType == typeof(SqliteFileMetadataStoreOptions))) {
            if (_sqliteFileStoreConfigure != null) {
                _services.AddSingleton<SqliteFileMetadataStoreOptions>(_ => {
                    var options = new SqliteFileMetadataStoreOptions();
                    _sqliteFileStoreConfigure(options);
                    return options;
                });
            }
            else {
                _services.AddSingleton<SqliteFileMetadataStoreOptions>(provider => {
                    var configuration = provider.GetRequiredService<IConfiguration>();
                    var options = LyoOptions.Bind<SqliteFileMetadataStoreOptions>(configuration, configSectionName);

                    return options;
                });
            }
        }

        if (!_services.Any(s => s.ServiceType == typeof(IDbContextFactory<SqliteFileMetadataStoreDbContext>))) {
            if (_sqliteFileStoreConfigure != null || _sqliteFileStoreConfigSection != null) {
                SqliteFileMetadataStoreOptions options;
                if (_sqliteFileStoreConfigure != null) {
                    options = new();
                    _sqliteFileStoreConfigure(options);
                }
                else {
                    // The builder has no service provider of its own, so it stands one up to reach IConfiguration.
                    using var tempProvider = _services.BuildServiceProvider();
                    options = LyoOptions.Bind<SqliteFileMetadataStoreOptions>(tempProvider.GetRequiredService<IConfiguration>(), configSectionName);
                }

                _services.AddSqliteFileMetadataStoreDbContextFactory(options);
            }
        }

        if (!_services.Any(s => s.ServiceKey != null && s.ServiceKey.Equals(_keyName) && s.ServiceType == typeof(IFileMetadataStore))) {
            _services.AddKeyedScoped<SqliteFileMetadataStore>(
                _keyName, (provider, _) => {
                    var factory = provider.GetRequiredService<IDbContextFactory<SqliteFileMetadataStoreDbContext>>();
                    var dbContext = factory.CreateDbContext();
                    var loggerFactory = provider.GetService<ILoggerFactory>();
                    return new(dbContext, loggerFactory);
                });

            _services.AddKeyedScoped<IFileMetadataStore>(_keyName, (provider, _) => provider.GetRequiredKeyedService<SqliteFileMetadataStore>(_keyName));
        }

        if (!_services.Any(s => s.ServiceType == typeof(IMultipartUploadSessionStore)))
            _services.AddSqliteMultipartUploadSessionStore();

        return _services;
    }
}