using Lyo.FileStorage.Abstractions;
using Microsoft.Extensions.Configuration;

namespace Lyo.FileStorage.Ftp;

/// <summary>Creates an FTP store from a <c>Provider: Ftp</c> section.</summary>
public sealed class FtpFileStorageStoreProvider : IFileStorageStoreProvider
{
    /// <inheritdoc />
    public string ProviderName => "Ftp";

    /// <inheritdoc />
    public Func<IServiceProvider, IFileStorageService> Create(string storeName, IConfiguration section)
    {
        var builder = new FtpFileStorageBuilder();
        builder.ApplySection(section);
        builder.EnsureValid();
        return builder.ToDelegate(storeName);
    }
}