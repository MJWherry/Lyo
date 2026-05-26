using Lyo.Common.Extensions;

namespace Lyo.FileStorage;

/// <summary>
/// Builds canonical cloud storage object/blob keys shared by <see cref="LocalFileStorageService" />, the S3 backend, and the Azure Blob backend. The key layout is
/// <c>[storagePrefix/][pathPrefix or shard]/{fileId:N}{extension}</c>, where <c>shard</c> is <c>{fileId[..2]}/{fileId[2..4]}</c> when no explicit <paramref name="pathPrefix" /> is
/// supplied.
/// </summary>
public static class CloudObjectKeyBuilder
{
    /// <summary>Build a canonical key consistent across cloud backends.</summary>
    /// <param name="fileId">Logical file identifier.</param>
    /// <param name="extension">Optional file extension (including leading dot, e.g. <c>.gz</c> or <c>.enc</c>). Empty string when raw.</param>
    /// <param name="pathPrefix">Explicit caller-supplied prefix path; when null/whitespace the per-file shard pair is used instead.</param>
    /// <param name="storagePrefix">Optional global storage prefix (e.g. <c>S3FileStorageOptions.KeyPrefix</c> or <c>BlobFileStorageOptions.BlobPrefix</c>).</param>
    public static string Build(Guid fileId, string extension = "", string? pathPrefix = null, string? storagePrefix = null)
    {
        var idString = fileId.ToString("N");
        var fileName = idString + (extension ?? "");
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
}