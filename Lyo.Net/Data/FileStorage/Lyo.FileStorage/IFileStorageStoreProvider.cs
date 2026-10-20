using Lyo.FileStorage.Abstractions;
using Microsoft.Extensions.Configuration;

namespace Lyo.FileStorage;

/// <summary>Builds one <see cref="IFileStorageService" /> from a store section. Provider packages register an implementation.</summary>
public interface IFileStorageStoreProvider
{
    /// <summary>Value of the <c>Provider</c> key, compared ordinal-ignore-case. Disk, S3, AzureBlob, Ftp, or Sftp.</summary>
    string ProviderName { get; }

    /// <summary>Returns a delegate that constructs the store from <paramref name="section" />. Reserved keys are read by the builder, not by the provider.</summary>
    Func<IServiceProvider, IFileStorageService> Create(string storeName, IConfiguration section);
}
