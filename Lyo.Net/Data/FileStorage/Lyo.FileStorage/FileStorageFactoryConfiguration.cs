using Lyo.FileStorage.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.FileStorage;

internal static class FileStorageFactoryConfiguration
{
    public static void Apply(IServiceCollection services, FileStorageFactoryCatalog catalog, IConfiguration section)
    {
        if (section[FileStorageFactoryKeys.Default] is { } defaultName && !string.IsNullOrWhiteSpace(defaultName))
            catalog.DefaultName = defaultName.Trim();

        foreach (var child in section.GetSection(FileStorageFactoryKeys.Stores).GetChildren()) {
            var providerName = child["Provider"];
            if (string.IsNullOrWhiteSpace(providerName))
                throw new InvalidOperationException($"File storage '{child.Key}' is missing Provider.");

            var provider = Match(services, providerName.Trim())
                ?? throw new InvalidOperationException($"File storage '{child.Key}' provider '{providerName.Trim()}' is not registered.");
            catalog.Add(new() {
                Name = FileStorageFactoryBuilder.RequireName(child.Key),
                Create = provider is IFileStorageProviderServices withServices ? withServices.Create(services, child.Key, child) : provider.Create(child.Key, child),
                EncryptionServiceKey = child[FileStorageFactoryKeys.EncryptionServiceKey],
                DisposeCreated = true
            });
        }
    }

    private static IFileStorageStoreProvider? Match(IServiceCollection services, string providerName)
    {
        foreach (var descriptor in services.Where(candidate => candidate.ServiceType == typeof(IFileStorageStoreProvider))) {
            var provider = descriptor.ImplementationInstance as IFileStorageStoreProvider
                ?? (descriptor.ImplementationType == null ? null : Activator.CreateInstance(descriptor.ImplementationType) as IFileStorageStoreProvider);
            if (provider != null && string.Equals(provider.ProviderName, providerName, StringComparison.OrdinalIgnoreCase))
                return provider;
        }

        return null;
    }
}
