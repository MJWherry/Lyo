using Lyo.FileStorage.Abstractions;
using Microsoft.Extensions.Configuration;

namespace Lyo.FileStorage.AzureBlob;

/// <summary>Creates an Azure Blob store from a <c>Provider: AzureBlob</c> section. Multipart stays off unless code calls <see cref="AzureBlobFileStorageBuilder.WithMultipart" />.</summary>
public sealed class AzureBlobFileStorageStoreProvider : IFileStorageStoreProvider
{
    /// <inheritdoc />
    public string ProviderName => "AzureBlob";

    /// <inheritdoc />
    public Func<IServiceProvider, IFileStorageService> Create(string storeName, IConfiguration section)
    {
        var builder = new AzureBlobFileStorageBuilder();
        builder.ApplySection(section);
        builder.EnsureValid();
        return builder.ToDelegate(storeName);
    }
}
