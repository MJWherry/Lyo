using Lyo.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.FileMetadataStore;

/// <summary>
/// Builder for configuring local file metadata store. Usage examples: // Use configuration section: services.AddLocalFileMetadataStoreKeyed("local-metadata")
/// .ConfigureLocalFileStore("LocalFileMetadataStore") .Build(); // Use action to configure: services.AddLocalFileMetadataStoreKeyed("local-metadata") .ConfigureLocalFileStore(options
/// => { options.RootDirectoryPath = "/path/to/metadata"; options.CreateDirectoryIfNotExists = true; }) .Build();
/// </summary>
public sealed class LocalFileMetadataStoreBuilder
{
    private readonly string _keyName;
    private readonly IServiceCollection _services;
    private string? _localFileMetadataStoreConfigSection;
    private Action<LocalFileMetadataStoreOptions>? _localFileMetadataStoreConfigure;

    internal LocalFileMetadataStoreBuilder(IServiceCollection services, string keyName)
    {
        _services = ArgumentHelpers.ThrowIfNullReturn(services, nameof(services));
        _keyName = ArgumentHelpers.ThrowIfNullReturn(keyName, nameof(keyName));
    }

    /// <summary>Configures local file metadata store using a configuration section name.</summary>
    /// <param name="configSectionName">The configuration section name (defaults to LocalFileMetadataStoreOptions.SectionName)</param>
    /// <returns>The builder for chaining</returns>
    public LocalFileMetadataStoreBuilder ConfigureLocalFileStore(string configSectionName = LocalFileMetadataStoreOptions.SectionName)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName, nameof(configSectionName));
        _localFileMetadataStoreConfigSection = configSectionName;
        return this;
    }

    /// <summary>Configures local file metadata store using an action.</summary>
    /// <param name="configure">Action to configure the options</param>
    /// <returns>The builder for chaining</returns>
    public LocalFileMetadataStoreBuilder ConfigureLocalFileStore(Action<LocalFileMetadataStoreOptions> configure)
    {
        ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
        _localFileMetadataStoreConfigure = configure;
        return this;
    }

    /// <summary>Builds and registers the local file metadata store.</summary>
    /// <returns>The service collection for chaining</returns>
    public IServiceCollection Build()
    {
        // Configure Local File Metadata Store Options
        var configSectionName = _localFileMetadataStoreConfigSection ?? LocalFileMetadataStoreOptions.SectionName;
        if (!_services.Any(s => s.ServiceType == typeof(LocalFileMetadataStoreOptions))) {
            if (_localFileMetadataStoreConfigure != null) {
                _services.AddSingleton<LocalFileMetadataStoreOptions>(_ => {
                    var options = new LocalFileMetadataStoreOptions();
                    _localFileMetadataStoreConfigure(options);
                    return options;
                });
            }
            else {
                _services.AddSingleton<LocalFileMetadataStoreOptions>(provider => {
                    var configuration = provider.GetRequiredService<IConfiguration>();
                    var section = configuration.GetSection(configSectionName);
                    var options = new LocalFileMetadataStoreOptions();
                    if (section.Exists())
                        section.Bind(options);

                    return options;
                });
            }
        }

        // Register the keyed metadata store
        if (!_services.Any(s => s.ServiceKey != null && s.ServiceKey.Equals(_keyName) && s.ServiceType == typeof(IFileMetadataStore))) {
            _services.AddKeyedSingleton<LocalFileMetadataStore>(
                _keyName, (provider, _) => {
                    var options = provider.GetRequiredService<LocalFileMetadataStoreOptions>();
                    ArgumentHelpers.ThrowIfNullOrWhiteSpace(options.RootDirectoryPath, nameof(options.RootDirectoryPath));
                    var loggerFactory = provider.GetService<ILoggerFactory>();
                    return new(options.RootDirectoryPath, loggerFactory);
                });

            _services.AddKeyedSingleton<IFileMetadataStore>(_keyName, (provider, _) => provider.GetRequiredKeyedService<LocalFileMetadataStore>(_keyName));
        }

        return _services;
    }
}