using Lyo.Common.Extensions;

namespace Lyo.FileStorage;

/// <summary>
/// Builds canonical cloud storage object/blob keys shared by <see cref="LocalFileStorageService" />, the S3 backend, and the Azure Blob backend. The key layout is
/// <c>[storagePrefix/][pathPrefix or shard]/{fileId:N}{extension}</c>, where <c>shard</c> is <c>{fileId[..2]}/{fileId[2..4]}</c> when no explicit path prefix is supplied to
/// <see cref="Build(Guid, string, string?, string?)" />. Use <see cref="FromMetadata" /> when the stored name is <c>SourceFileName</c> from file metadata.
/// </summary>
public static class CloudObjectKeyBuilder
{
    /// <summary>Build a canonical key consistent across cloud backends.</summary>
    /// <param name="fileId">Logical file identifier.</param>
    /// <param name="extension">Optional file extension (including leading dot, e.g. <c>.gz</c> or <c>.enc</c>). Empty string when raw.</param>
    /// <param name="pathPrefix">Explicit caller-supplied prefix path; when null/whitespace the per-file shard pair is used instead.</param>
    /// <param name="storagePrefix">Optional global storage prefix (e.g. <c>S3FileStorageOptions.KeyPrefix</c> or <c>AzureBlobFileStorageOptions.BlobPrefix</c>).</param>
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
    /// Derives trailing characters from <paramref name="sourceFileName" /> after stripping the GUID prefix so hashed storage layouts preserve extensions and extra suffix
    /// segments (for example <c>.gz</c> or <c>.enc</c>).
    /// </summary>
    /// <param name="fileId">Logical file identifier used when matching prefixed filenames.</param>
    /// <param name="sourceFileName">Stored filename optionally beginning with the file id in <c>N</c> (no hyphens) or default <c>D</c> (with hyphens) format.</param>
    /// <returns>Suffix following the GUID prefix, or an empty string when indeterminate.</returns>
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

    /// <summary>Builds the expected object key from metadata fields (<see cref="Lyo.FileMetadataStore.Models.FileStoreResult.SourceFileName" /> and path prefix).</summary>
    /// <param name="fileId">Logical file identifier.</param>
    /// <param name="sourceFileName">Stored object name, typically <c>{fileId:N}{suffix}</c>.</param>
    /// <param name="pathPrefix">Explicit caller-supplied prefix path; when null/whitespace the per-file shard pair is used instead.</param>
    /// <param name="storagePrefix">Optional global storage prefix (bucket key prefix / blob prefix). Leave null when comparing keys relative to the backend root.</param>
    public static string FromMetadata(Guid fileId, string? sourceFileName, string? pathPrefix, string? storagePrefix = null)
        => Build(fileId, InferTrailingSuffixAfterFileId(fileId, sourceFileName), pathPrefix, storagePrefix);
}