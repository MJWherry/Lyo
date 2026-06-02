using Lyo.Exceptions;
using Lyo.FileMetadataStore.Sqlite.Database;
using Lyo.FileStorage.Multipart;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.FileMetadataStore.Sqlite;

/// <summary>Builder for configuring SQLite file metadata store with its dependencies.</summary>
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

    /// <summary>Configures SQLite file store options using a configuration section name.</summary>
    /// <param name="configSectionName">The configuration section name (defaults to <see cref="SqliteFileMetadataStoreOptions.SectionName" />)</param>
    /// <returns>The builder for chaining</returns>
    public SqliteFileMetadataStoreBuilder ConfigureSqliteFileStore(string configSectionName = SqliteFileMetadataStoreOptions.SectionName)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
        _sqliteFileStoreConfigSection = configSectionName;
        return this;
    }

    /// <summary>Configures SQLite file store options using an action.</summary>
    /// <param name="configure">Action to configure the options</param>
    /// <returns>The builder for chaining</returns>
    public SqliteFileMetadataStoreBuilder ConfigureSqliteFileStore(Action<SqliteFileMetadataStoreOptions> configure)
    {
        ArgumentHelpers.ThrowIfNull(configure);
        _sqliteFileStoreConfigure = configure;
        return this;
    }

    /// <summary>Builds and registers the SQLite file metadata store.</summary>
    /// <returns>The service collection for chaining</returns>
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
                    var section = configuration.GetSection(configSectionName);
                    var options = new SqliteFileMetadataStoreOptions();
                    if (section.Exists())
                        section.Bind(options);

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
                    using var tempProvider = _services.BuildServiceProvider();
                    var configuration = tempProvider.GetRequiredService<IConfiguration>();
                    var section = configuration.GetSection(configSectionName);
                    options = new();
                    if (section.Exists())
                        section.Bind(options);
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
