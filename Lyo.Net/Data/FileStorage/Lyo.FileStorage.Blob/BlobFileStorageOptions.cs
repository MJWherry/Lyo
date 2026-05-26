using Azure.Storage.Blobs.Models;
using Lyo.FileStorage.Models;

namespace Lyo.FileStorage.Blob;

/// <summary>Configuration for <see cref="BlobFileStorageService" /> backed by Azure Blob Storage.</summary>
public sealed class BlobFileStorageOptions : FileStorageServiceBaseOptions
{
    public const string SectionName = "BlobFileStorage";

    /// <summary>Legacy subsection name when migrating from the pre-<c>Lyo.FileStorage.Blob</c> packages.</summary>
    public const string LegacyAzureConfigurationSectionName = "AzureFileStorageOptions";

    /// <summary>The connection string for the Azure Storage account.</summary>
    public string ConnectionString { get; set; } = null!;

    /// <summary>The name of the blob container where files will be stored.</summary>
    public string ContainerName { get; set; } = null!;

    /// <summary>Optional prefix to use for all blob names. Useful for organizing files in a container.</summary>
    public string? BlobPrefix { get; set; }

    /// <summary>Azure Blobs encryption scope (server-side).</summary>
    public string? EncryptionScope { get; set; }

    /// <summary>Base64-encoded 256-bit AES key for customer-provided key (SSE-C). Applies to SDK uploads. Presigned/direct PUT uploads with SSE-C are not supported and will fail fast.</summary>
    public string? CustomerProvidedKeyBase64 { get; set; }

    public bool UsesCustomerProvidedKey => !string.IsNullOrWhiteSpace(CustomerProvidedKeyBase64);

    internal CustomerProvidedKey? ResolveCustomerProvidedKey()
    {
        if (string.IsNullOrWhiteSpace(CustomerProvidedKeyBase64))
            return null;

        var bytes = Convert.FromBase64String(CustomerProvidedKeyBase64.Trim());
        return new CustomerProvidedKey(bytes);
    }
}