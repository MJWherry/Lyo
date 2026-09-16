using Lyo.Exceptions;
using Lyo.Compression;
using Lyo.Compression.Models;

namespace Lyo.FileManager;

/// <summary>Per-upload compression and encryption choices. The local host reads these while a save is in flight.</summary>
internal static class FileManagerUploadScope
{
    private static readonly AsyncLocal<string?> Compression = new();
    private static readonly AsyncLocal<string?> Encryption = new();

    /// <summary>Algorithm name for the upload on this async flow. Null keeps the host default.</summary>
    public static string? CompressionAlgorithm => Compression.Value;

    /// <summary>Algorithm name for the upload on this async flow. Null keeps AES-SIV.</summary>
    public static string? EncryptionAlgorithm => Encryption.Value;

    /// <summary>Applies <paramref name="options" /> until the returned scope is disposed.</summary>
    public static IDisposable Push(FileManagerUploadOptions options)
    {
        ArgumentHelpers.ThrowIfNull(options);
        var previousCompression = Compression.Value;
        var previousEncryption = Encryption.Value;
        Compression.Value = options.CompressionAlgorithm;
        Encryption.Value = options.EncryptionAlgorithm;
        return new Pop(previousCompression, previousEncryption);
    }

    private sealed class Pop(string? compression, string? encryption) : IDisposable
    {
        public void Dispose()
        {
            Compression.Value = compression;
            Encryption.Value = encryption;
        }
    }
}

/// <summary>Uses the upload's compression algorithm when one was chosen. Otherwise Brotli.</summary>
internal sealed class UploadCompressionSelector : ICompressionAlgorithmSelector
{
    /// <inheritdoc />
    public CompressionSelectionResult ResolveForCompress(CompressionSelectionContext context)
    {
        ArgumentHelpers.ThrowIfNull(context);
        var chosen = CompressionAlgorithm.TryFromName(FileManagerUploadScope.CompressionAlgorithm);
        return new(true, chosen ?? CompressionAlgorithm.Brotli);
    }
}
