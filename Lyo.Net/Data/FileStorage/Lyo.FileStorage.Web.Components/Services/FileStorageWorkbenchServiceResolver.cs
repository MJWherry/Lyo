using Lyo.FileStorage;
using Lyo.Keystore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lyo.FileStorage.Web.Components.Services;

public sealed class FileStorageWorkbenchServiceResolver
{
    private readonly FileStorageWorkbenchOptions _options;
    private readonly IServiceProvider _services;

    public FileStorageWorkbenchServiceResolver(IServiceProvider services, IOptions<FileStorageWorkbenchOptions> options)
    {
        _services = services;
        _options = options.Value;
    }

    public IFileStorageService? TryGetFileStorageService() => ResolveOptional<IFileStorageService>(_options.FileStorageServiceKey);

    public IKeyStore? TryGetKeyStore() => ResolveOptional<IKeyStore>(_options.KeyStoreServiceKey);

    public IFileStorageWorkbenchQueryService? TryGetQueryService() => _services.GetService<IFileStorageWorkbenchQueryService>();

    public string DescribeFileStorageResolution() => Describe(typeof(IFileStorageService).Name, _options.FileStorageServiceKey);

    public string DescribeKeyStoreResolution() => Describe(typeof(IKeyStore).Name, _options.KeyStoreServiceKey);

    private T? ResolveOptional<T>(string? serviceKey)
        where T : class
        => string.IsNullOrWhiteSpace(serviceKey) ? _services.GetService<T>() : _services.GetKeyedService<T>(serviceKey);

    private static string Describe(string serviceName, string? serviceKey)
        => string.IsNullOrWhiteSpace(serviceKey) ? $"{serviceName} (default DI registration)" : $"{serviceName} (keyed: {serviceKey})";
}
