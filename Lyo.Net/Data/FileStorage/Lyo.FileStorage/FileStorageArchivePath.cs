using Lyo.Common.Pathing;
using Lyo.Exceptions;
using Lyo.FileMetadataStore.Models;

namespace Lyo.FileStorage;

/// <summary>Zip download names and relative entry paths for <see cref="Abstractions.IFileStorageArchiveService" />. Delegates sanitizing and path shape to <see cref="PathHelpers" />.</summary>
public static class FileStorageArchivePath
{
    /// <summary>Strips path segments and invalid filename characters, then ensures a <c>.zip</c> extension. Defaults to <c>files.zip</c>.</summary>
    public static string SanitizeZipFileName(string? fileName)
    {
        var source = string.IsNullOrWhiteSpace(fileName) ? "files" : fileName!.Trim();
        var leaf = PathHelpers.GetFileName(PathStyle.Posix, PathHelpers.NormalizeSeparators(source, PathStyle.Posix));
        var sanitized = PathHelpers.SanitizeFileName(leaf) ?? "files";
        if (sanitized.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            return sanitized;

        return sanitized + ".zip";
    }

    /// <summary>
    /// Normalizes a relative zip path to forward slashes. Rejects empty, rooted, or <c>..</c> segments. When <paramref name="zipPath" /> is null or whitespace, uses
    /// <paramref name="fallbackFileName" /> (then the file id).
    /// </summary>
    public static string NormalizeZipPath(string? zipPath, Guid fileId, string? fallbackFileName = null)
    {
        var raw = string.IsNullOrWhiteSpace(zipPath) ? fallbackFileName : zipPath;
        if (string.IsNullOrWhiteSpace(raw))
            raw = fileId.ToString("D");

        var posix = PathHelpers.NormalizeSeparators(raw!.Trim(), PathStyle.Posix);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(posix, nameof(zipPath));
        if (PathHelpers.IsPathRooted(PathStyle.Posix, posix) || posix.Contains(':'))
            throw new ArgumentException($"Zip path is not a relative path: {raw}", nameof(zipPath));

        posix = posix.Trim('/');
        var parts = posix.Split(['/'], StringSplitOptions.RemoveEmptyEntries);
        OperationHelpers.ThrowIf(parts.Length == 0, "Zip path is empty.");
        for (var i = 0; i < parts.Length; i++) {
            if (parts[i] is "." or "..")
                throw new ArgumentException($"Zip path must not contain '.' or '..' segments: {raw}", nameof(zipPath));

            parts[i] = PathHelpers.SanitizeFileName(parts[i]) ?? "file";
        }

        return string.Join("/", parts);
    }

    /// <summary>Replaces invalid filename characters. Empty results become <c>file</c>.</summary>
    public static string SanitizePathSegment(string? name) => PathHelpers.SanitizeFileName(name) ?? "file";

    /// <summary>Appends an extension from metadata when the last zip-path segment has none.</summary>
    public static string EnsureExtension(string zipPath, FileStoreResult metadata)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(zipPath);
        var leaf = PathHelpers.GetFileName(PathStyle.Posix, zipPath);
        if (!string.IsNullOrEmpty(PathHelpers.GetExtension(PathStyle.Posix, leaf)))
            return zipPath;

        return zipPath + ExtensionFromMetadata(metadata);
    }

    /// <summary>Prefers the original filename extension, then a well-known content type, then <c>.bin</c>.</summary>
    public static string ExtensionFromMetadata(FileStoreResult metadata)
    {
        ArgumentHelpers.ThrowIfNull(metadata);
        if (!string.IsNullOrWhiteSpace(metadata.OriginalFileName)) {
            var ext = PathHelpers.GetExtension(PathStyle.Posix, metadata.OriginalFileName!);
            if (!string.IsNullOrWhiteSpace(ext) && ext.Length <= 8)
                return ext;
        }

        return metadata.ContentType?.ToLowerInvariant() switch {
            "image/jpeg" or "image/jpg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            "image/gif" => ".gif",
            "image/avif" => ".avif",
            "application/pdf" => ".pdf",
            "text/plain" => ".txt",
            "application/json" => ".json",
            _ => ".bin"
        };
    }
}
