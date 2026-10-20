using Lyo.FileStorage.Abstractions;
using Microsoft.Extensions.Configuration;

namespace Lyo.FileStorage;

/// <summary>Creates a disk store from a <c>Provider: Disk</c> section.</summary>
public sealed class DiskFileStorageStoreProvider : IFileStorageStoreProvider
{
    /// <inheritdoc />
    public string ProviderName => "Disk";

    /// <inheritdoc />
    public Func<IServiceProvider, IFileStorageService> Create(string storeName, IConfiguration section)
    {
        var builder = new DiskFileStorageBuilder();
        builder.ApplySection(section);
        builder.EnsureValid();
        return builder.ToDelegate(storeName);
    }
}
