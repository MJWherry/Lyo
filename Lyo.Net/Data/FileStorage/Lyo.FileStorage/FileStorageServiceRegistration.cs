using Lyo.Exceptions;
using Lyo.FileStorage.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.FileStorage;

/// <summary>Shared DI wiring for file-storage provider packages (FTP, SFTP, Azure Blob, and similar).</summary>
public static class FileStorageServiceRegistration
{
    /// <summary>Registers a scoped provider service and the <see cref="IFileStorageService" /> facade that resolves it.</summary>
    public static void AddScopedFileStorage<TService>(IServiceCollection services, Func<IServiceProvider, TService> factory)
        where TService : class, IFileStorageService
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNull(factory);
        services.AddScoped(factory);
        services.AddScoped<IFileStorageService>(sp => sp.GetRequiredService<TService>());
    }
}
