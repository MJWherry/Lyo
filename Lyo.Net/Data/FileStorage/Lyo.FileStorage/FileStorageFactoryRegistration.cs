using Lyo.FileStorage.Abstractions;

namespace Lyo.FileStorage;

internal sealed class FileStorageFactoryRegistration
{
    public required string Name { get; init; }

    public IFileStorageService? Instance { get; init; }

    public Func<IServiceProvider, IFileStorageService>? Create { get; init; }

    public string? EncryptionServiceKey { get; init; }

    public bool DisposeCreated { get; init; }
}