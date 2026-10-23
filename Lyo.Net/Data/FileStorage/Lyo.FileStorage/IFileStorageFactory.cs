using Lyo.FileStorage.Abstractions;

namespace Lyo.FileStorage;

/// <summary>
/// Named <see cref="IFileStorageService" /> instances registered at startup. Resolve one with <see cref="Get" />. The factory is scoped: a delegate registration runs once
/// per scope, and the factory disposes services it created when the scope ends.
/// </summary>
public interface IFileStorageFactory
{
    /// <summary>Registered store names, ordinal-sorted.</summary>
    IReadOnlyCollection<string> Names { get; }

    /// <summary>Store name served by the bare file-storage route when <c>FileStorage:Default</c> is set. Null when no default was configured.</summary>
    string? DefaultName { get; }

    /// <summary>Returns the store registered under <paramref name="name" />.</summary>
    /// <param name="name">Store name. Matching is case-insensitive.</param>
    /// <exception cref="InvalidOperationException">The name is blank or no store is registered under it.</exception>
    IFileStorageService Get(string name);

    /// <summary>Returns false when <paramref name="name" /> is blank or not registered.</summary>
    bool TryGet(string name, out IFileStorageService? service);

    /// <summary>Encryption service key recorded for <paramref name="name" />, or null when that store omitted encryption.</summary>
    /// <exception cref="InvalidOperationException">No store is registered under <paramref name="name" />.</exception>
    string? GetEncryptionServiceKey(string name);
}