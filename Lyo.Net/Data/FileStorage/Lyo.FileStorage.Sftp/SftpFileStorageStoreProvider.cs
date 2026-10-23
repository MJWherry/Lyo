using Lyo.FileStorage.Abstractions;
using Microsoft.Extensions.Configuration;

namespace Lyo.FileStorage.Sftp;

/// <summary>Creates an SFTP store from a <c>Provider: Sftp</c> section.</summary>
public sealed class SftpFileStorageStoreProvider : IFileStorageStoreProvider
{
    /// <inheritdoc />
    public string ProviderName => "Sftp";

    /// <inheritdoc />
    public Func<IServiceProvider, IFileStorageService> Create(string storeName, IConfiguration section)
    {
        var builder = new SftpFileStorageBuilder();
        builder.ApplySection(section);
        builder.EnsureValid();
        return builder.ToDelegate(storeName);
    }
}