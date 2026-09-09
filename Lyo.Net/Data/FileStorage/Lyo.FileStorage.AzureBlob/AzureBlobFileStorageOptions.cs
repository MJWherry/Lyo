using Azure.Storage.Blobs.Models;
using Lyo.Exceptions;
using Lyo.FileStorage.Models;

namespace Lyo.FileStorage.AzureBlob;

/// <summary>Options for <see cref="AzureBlobFileStorageService" /> over Azure Blob Storage.</summary>
public sealed class AzureBlobFileStorageOptions : FileStorageServiceBaseOptions
{
    public const string SectionName = "AzureBlobFileStorage";

    /// <summary>Obsolete section name from <c>Lyo.FileStorage.Blob</c> before the AzureBlob rename.</summary>
    public const string LegacyBlobConfigurationSectionName = "BlobFileStorage";

    /// <summary>Obsolete subsection name when migrating from the pre-<c>Lyo.FileStorage.AzureBlob</c> packages.</summary>
    public const string LegacyAzureConfigurationSectionName = "AzureFileStorageOptions";

    /// <summary>Azure Storage account connection string.</summary>
    public string ConnectionString { get; set; } = null!;

    /// <summary>Blob container that holds stored files.</summary>
    public string ContainerName { get; set; } = null!;

    /// <summary>Optional prefix applied to every blob name (useful for grouping files in a container).</summary>
    public string? BlobPrefix { get; set; }

    /// <summary>Azure Blobs server-side encryption scope.</summary>
    public string? EncryptionScope { get; set; }

    /// <summary>Base64-encoded 256-bit AES key for customer-provided key (SSE-C). Applies to SDK uploads. Presigned/direct PUT uploads with SSE-C are not supported and fail fast.</summary>
    public string? CustomerProvidedKeyBase64 { get; set; }

    public bool UsesCustomerProvidedKey => !string.IsNullOrWhiteSpace(CustomerProvidedKeyBase64);

    internal CustomerProvidedKey? ResolveCustomerProvidedKey()
    {
        if (string.IsNullOrWhiteSpace(CustomerProvidedKeyBase64))
            return null;

        var bytes = Convert.FromBase64String(CustomerProvidedKeyBase64.Trim());
        return new CustomerProvidedKey(bytes);
    }

    /// <summary>Throws when ConnectionString or ContainerName is missing.</summary>
    public void Validate()
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(ConnectionString);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(ContainerName);
    }
}