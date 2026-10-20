using Lyo.Exceptions;
using Lyo.FileStorage.Abstractions;

namespace Lyo.FileStorage;

/// <summary>Scoped lookup of stores recorded by <see cref="FileStorageFactoryBuilder" />.</summary>
public sealed class FileStorageFactory : IFileStorageFactory, IDisposable
{
    private readonly Dictionary<string, IFileStorageService> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly FileStorageFactoryCatalog _catalog;
    private readonly List<IDisposable> _owned = [];
    private readonly object _gate = new();
    private readonly IServiceProvider _services;
    private bool _disposed;

    /// <summary>Creates a factory that resolves delegate registrations from <paramref name="services" />.</summary>
    public FileStorageFactory(IServiceProvider services, FileStorageFactoryCatalog catalog)
    {
        _services = ArgumentHelpers.ThrowIfNullReturn(services);
        _catalog = ArgumentHelpers.ThrowIfNullReturn(catalog);
    }

    /// <inheritdoc />
    public IReadOnlyCollection<string> Names => _catalog.Names;

    /// <inheritdoc />
    public string? DefaultName => _catalog.DefaultName;

    /// <inheritdoc />
    public IFileStorageService Get(string name)
    {
        if (!TryGet(name, out var service))
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(name)
                ? "A file storage name is required."
                : $"No file storage service is registered under '{name}'.");

        return service!;
    }

    /// <inheritdoc />
    public bool TryGet(string name, out IFileStorageService? service)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(FileStorageFactory));
        service = null;
        if (string.IsNullOrWhiteSpace(name) || !_catalog.TryFind(name, out var registration))
            return false;

        lock (_gate) {
            if (_cache.TryGetValue(registration.Name, out service))
                return true;

            service = registration.Instance ?? registration.Create!(_services);
            _cache[registration.Name] = service;
            if (registration.DisposeCreated && service is IDisposable disposable)
                _owned.Add(disposable);
        }

        return true;
    }

    /// <inheritdoc />
    public string? GetEncryptionServiceKey(string name)
    {
        if (!_catalog.TryFind(name, out var registration))
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(name)
                ? "A file storage name is required."
                : $"No file storage service is registered under '{name}'.");

        return registration.EncryptionServiceKey;
    }

    /// <summary>Disposes stores this scope created. Instances passed to <c>Register</c> are left alone.</summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        lock (_gate) {
            foreach (var owned in _owned)
                owned.Dispose();

            _owned.Clear();
            _cache.Clear();
        }
    }
}
