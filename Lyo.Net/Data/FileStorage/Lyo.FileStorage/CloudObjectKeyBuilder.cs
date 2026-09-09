using Lyo.Common.Core.Extensions;

namespace Lyo.FileStorage;

/// <summary>
/// Builds canonical cloud object/blob keys shared by <see cref="LocalFileStorageService" />, the S3 backend, and the Azure Blob backend. The key layout is
/// <c>[storagePrefix/][pathPrefix or shard]/{fileId:N}{extension}</c>, where <c>shard</c> is <c>{fileId[..2]}/{fileId[2..4]}</c> when no explicit path prefix is passed to
/// <see cref="Build(Guid, string, string?, string?)" />. Use <see cref="FromMetadata" /> when the stored name is <c>SourceFileName</c> from file metadata.
/// </summary>
public static class CloudObjectKeyBuilder
{
    /// <summary>Builds a canonical key that matches across cloud backends.</summary>
    /// <param name="fileId">Logical file identifier.</param>
    /// <param name="extension">Optional file extension, including the leading dot (for example <c>.gz</c> or <c>.enc</c>). Empty string when raw.</param>
    /// <param name="pathPrefix">Caller-supplied prefix path. When null or whitespace, the per-file shard pair is used instead.</param>
    /// <param name="storagePrefix">Optional global storage prefix (for example <c>S3FileStorageOptions.KeyPrefix</c> or <c>AzureBlobFileStorageOptions.BlobPrefix</c>).</param>
    public static string Build(Guid fileId, string extension = "", string? pathPrefix = null, string? storagePrefix = null)
    {
        var idString = fileId.ToString("N");
        var fileName = idString + extension;
        var parts = new List<string>(4);
        if (!storagePrefix.IsNullOrWhitespace())
            parts.Add(storagePrefix.Trim().TrimStart('/', '\\').TrimEnd('/', '\\'));

        if (!pathPrefix.IsNullOrWhitespace())
            parts.Add(pathPrefix);
        else {
            parts.Add(idString[..2]);
            parts.Add(idString.Substring(2, 2));
        }

        parts.Add(fileName);
        return string.Join("/", parts);
    }

    /// <summary>
    /// Takes the trailing characters of <paramref name="sourceFileName" /> after the GUID prefix so hashed storage layouts keep extensions and extra suffix
    /// segments (for example <c>.gz</c> or <c>.enc</c>).
    /// </summary>
    /// <param name="fileId">Logical file identifier used when matching prefixed filenames.</param>
    /// <param name="sourceFileName">Stored filename that may start with the file id in <c>N</c> (no hyphens) or default <c>D</c> (with hyphens) format.</param>
    /// <returns>Suffix after the GUID prefix, or an empty string when it cannot be determined.</returns>
    public static string InferTrailingSuffixAfterFileId(Guid fileId, string? sourceFileName)
    {
        if (sourceFileName.IsNullOrEmpty())
            return "";

        var n = fileId.ToString("N");
        if (sourceFileName.StartsWith(n, StringComparison.Ordinal))
            return sourceFileName[n.Length..];

        var dash = fileId.ToString();
        return sourceFileName.StartsWith(dash, StringComparison.OrdinalIgnoreCase) ? sourceFileName[dash.Length..] : "";
    }

    /// <summary>Builds the expected object key from metadata (<see cref="Lyo.FileMetadataStore.Models.FileStoreResult.SourceFileName" /> and path prefix).</summary>
    /// <param name="fileId">Logical file identifier.</param>
    /// <param name="sourceFileName">Stored object name, typically <c>{fileId:N}{suffix}</c>.</param>
    /// <param name="pathPrefix">Caller-supplied prefix path. When null or whitespace, the per-file shard pair is used instead.</param>
    /// <param name="storagePrefix">Optional global storage prefix (bucket key prefix or blob prefix). Leave null when comparing keys relative to the backend root.</param>
    public static string FromMetadata(Guid fileId, string? sourceFileName, string? pathPrefix, string? storagePrefix = null)
        => Build(fileId, InferTrailingSuffixAfterFileId(fileId, sourceFileName), pathPrefix, storagePrefix);
}