namespace Lyo.FileStorage;

/// <summary>Startup list of stores. Filled while the service collection is built. Read by each scoped <see cref="FileStorageFactory" />.</summary>
public sealed class FileStorageFactoryCatalog
{
    private readonly List<FileStorageFactoryRegistration> _registrations = [];
    private readonly object _gate = new();

    /// <summary>Store served by the bare route. Null when <c>FileStorage:Default</c> was not set.</summary>
    public string? DefaultName { get; set; }

    /// <summary>Registered names, ordinal-sorted.</summary>
    public IReadOnlyCollection<string> Names
    {
        get {
            lock (_gate)
                return _registrations.Select(static registration => registration.Name).OrderBy(static name => name, StringComparer.Ordinal).ToArray();
        }
    }

    internal void Add(FileStorageFactoryRegistration registration)
    {
        lock (_gate) {
            if (_registrations.Any(existing => string.Equals(existing.Name, registration.Name, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"File storage '{registration.Name}' is already registered.");

            _registrations.Add(registration);
        }
    }

    internal bool TryFind(string name, out FileStorageFactoryRegistration registration)
    {
        lock (_gate) {
            registration = _registrations.FirstOrDefault(existing => string.Equals(existing.Name, name, StringComparison.OrdinalIgnoreCase))!;
            return registration != null;
        }
    }
}
