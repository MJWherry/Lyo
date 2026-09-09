namespace Lyo.FileStorage.Abstractions;

/// <summary>Diagnostic calls for backends that expose object keys or paths (disk, blob, S3-compatible).</summary>
public interface IFileStorageDiagnosticsService
{
    /// <summary>Lists at most <paramref name="maxKeys" /> storage locations under optional <paramref name="prefix" />, relative to the backend root or key prefix.</summary>
    Task<IReadOnlyList<string>> ListStorageKeysAsync(string? prefix = null, int maxKeys = 1000, CancellationToken ct = default);
}