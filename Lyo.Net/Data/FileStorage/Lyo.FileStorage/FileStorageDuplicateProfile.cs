using Lyo.Compression.Models;
using Lyo.FileMetadataStore.Models;

namespace Lyo.FileStorage;

/// <summary>Compares requested save-time compression/encryption options against stored metadata for duplicate detection.</summary>
internal static class FileStorageDuplicateProfile
{
    internal static bool Matches(FileStoreResult existing, bool compress, bool encrypt, string? keyId, CompressionAlgorithm? compressionAlgorithm)
    {
        if (existing.IsCompressed != compress)
            return false;

        if (existing.IsEncrypted != encrypt)
            return false;

        if (compress && existing.CompressionAlgorithm != compressionAlgorithm)
            return false;

        if (encrypt && !string.Equals(keyId?.Trim(), existing.DataEncryptionKeyId?.Trim(), StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }

    internal static string BuildMismatchMessage(
        Guid existingFileId,
        FileStoreResult existing,
        bool compress,
        bool encrypt,
        string? keyId,
        CompressionAlgorithm? compressionAlgorithm)
    {
        var storedProfile = FormatProfile(existing.IsCompressed, existing.IsEncrypted, existing.DataEncryptionKeyId, existing.CompressionAlgorithm);
        var requestedProfile = FormatProfile(compress, encrypt, keyId, compressionAlgorithm);
        return $"A file with the same content hash already exists (file ID '{existingFileId}') but with a different storage profile. " +
            $"Stored: {storedProfile}. Requested: {requestedProfile}. " +
            "Use DuplicateHandlingStrategy.Overwrite to replace the stored object with the requested profile, or AllowDuplicate to create a separate file.";
    }

    private static string FormatProfile(bool compress, bool encrypt, string? keyId, CompressionAlgorithm? algorithm)
    {
        var parts = new List<string> { $"compress={compress}" };
        if (compress && algorithm != null)
            parts.Add($"algorithm={algorithm.Name}");

        parts.Add($"encrypt={encrypt}");
        if (encrypt && !string.IsNullOrWhiteSpace(keyId))
            parts.Add($"keyId={keyId.Trim()}");

        return string.Join(", ", parts);
    }
}