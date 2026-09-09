using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;

namespace Lyo.FileStorage.AzureBlob;

/// <summary>
/// Applies <see cref="AzureBlobFileStorageOptions.EncryptionScope" /> and <see cref="AzureBlobFileStorageOptions.CustomerProvidedKeyBase64" /> to blob clients and SAS
/// builders. Existence probes, property reads, copies, and deletes must use these helpers: a request against a blob written with a customer-provided key fails without
/// the matching key headers, so a client built directly off the container breaks reads and lookups whenever SSE-C is configured.
/// </summary>
internal static class AzureBlobEncryptionClients
{
    /// <summary><see cref="BlobClient" /> for <paramref name="blobName" /> with encryption scope and customer-provided key applied.</summary>
    internal static BlobClient EncryptionAwareBlobClient(BlobContainerClient container, AzureBlobFileStorageOptions options, string blobName)
    {
        var client = container.GetBlobClient(blobName);
        if (!string.IsNullOrWhiteSpace(options.EncryptionScope))
            client = client.WithEncryptionScope(options.EncryptionScope);

        var cpk = options.ResolveCustomerProvidedKey();
        return cpk.HasValue ? client.WithCustomerProvidedKey(cpk.Value) : client;
    }

    /// <summary>Block-blob form of <see cref="EncryptionAwareBlobClient" /> for writes and staging that need block-level APIs.</summary>
    internal static BlockBlobClient EncryptionAwareBlockBlobClient(BlobContainerClient container, AzureBlobFileStorageOptions options, string blobName)
    {
        var client = container.GetBlockBlobClient(blobName);
        if (!string.IsNullOrWhiteSpace(options.EncryptionScope))
            client = client.WithEncryptionScope(options.EncryptionScope);

        var cpk = options.ResolveCustomerProvidedKey();
        return cpk.HasValue ? client.WithCustomerProvidedKey(cpk.Value) : client;
    }

    /// <summary>
    /// Copies the configured encryption scope onto <paramref name="sas" />. A customer-provided key cannot travel this way — it lives in request headers, which a bare
    /// URL cannot set — so SAS callers must reject <see cref="AzureBlobFileStorageOptions.UsesCustomerProvidedKey" /> instead.
    /// </summary>
    internal static void ApplyEncryptionScope(BlobSasBuilder sas, AzureBlobFileStorageOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.EncryptionScope))
            sas.EncryptionScope = options.EncryptionScope;
    }
}
