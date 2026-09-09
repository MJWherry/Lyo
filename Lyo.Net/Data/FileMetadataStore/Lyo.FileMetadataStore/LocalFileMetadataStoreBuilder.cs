using Lyo.Configuration;
using Lyo.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.FileMetadataStore;

/// <summary>
/// Fluent setup for a keyed local file metadata store. Bind from a configuration section via <c>ConfigureLocalFileStore("LocalFileMetadataStore")</c>, or mutate options in
/// <c>ConfigureLocalFileStore(options =&gt; …)</c>, then call <c>Build()</c>.
/// </summary>
public sealed class LocalFileMetadataStoreBuilder
{
    private readonly string _keyName;
    private readonly IServiceCollection _services;
    private string? _localFileMetadataStoreConfigSection;
    private Action<LocalFileMetadataStoreOptions>? _localFileMetadataStoreConfigure;

    internal LocalFileMetadataStoreBuilder(IServiceCollection services, string keyName)
    {
        _services = ArgumentHelpers.ThrowIfNullReturn(services);
        _keyName = ArgumentHelpers.ThrowIfNullReturn(keyName);
    }

    /// <summary>Binds options from a configuration section.</summary>
    /// <param name="configSectionName">Section name; starts as LocalFileMetadataStoreOptions.SectionName.</param>
    /// <returns>This builder for chaining.</returns>
    public LocalFileMetadataStoreBuilder ConfigureLocalFileStore(string configSectionName = LocalFileMetadataStoreOptions.SectionName)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
        _localFileMetadataStoreConfigSection = configSectionName;
        return this;
    }

    /// <summary>Mutates options via a callback.</summary>
    /// <param name="configure">Callback that sets options.</param>
    /// <returns>This builder for chaining.</returns>
    public LocalFileMetadataStoreBuilder ConfigureLocalFileStore(Action<LocalFileMetadataStoreOptions> configure)
    {
        ArgumentHelpers.ThrowIfNull(configure);
        _localFileMetadataStoreConfigure = configure;
        return this;
    }

    /// <summary>Registers the local file metadata store and returns the service collection.</summary>
    /// <returns>Service collection for chaining.</returns>
    public IServiceCollection Build()
    {
        // Bind or apply local-file metadata options
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
                    var options = LyoOptions.Bind<LocalFileMetadataStoreOptions>(configuration, configSectionName);

                    return options;
                });
            }
        }

        // Register the keyed store when missing
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