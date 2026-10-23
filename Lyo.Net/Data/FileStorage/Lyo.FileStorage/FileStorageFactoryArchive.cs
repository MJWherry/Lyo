using Lyo.FileStorage.Abstractions;
using Lyo.FileStorage.Models;
using Lyo.IO.Temp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.FileStorage;

internal static class FileStorageFactoryArchive
{
    public static void Register(IServiceCollection services, string storeName)
    {
        if (!services.Any(descriptor => descriptor.ServiceType == typeof(FileStorageArchiveOptions)))
            services.AddSingleton(new FileStorageArchiveOptions());

        services.TryAddIOTemp();
        services.AddKeyedScoped<IFileStorageArchiveService>(
            storeName,
            (provider, _) => new FileStorageArchiveService(
                provider.GetRequiredService<IFileStorageFactory>().Get(storeName), provider.GetRequiredService<IIOTempService>(),
                provider.GetRequiredService<FileStorageArchiveOptions>(), provider.GetService<ILogger<FileStorageArchiveService>>()));
    }

    private static void TryAddIOTemp(this IServiceCollection services)
    {
        if (!services.Any(descriptor => descriptor.ServiceType == typeof(IIOTempService)))
            services.AddIOTempService();
    }
}