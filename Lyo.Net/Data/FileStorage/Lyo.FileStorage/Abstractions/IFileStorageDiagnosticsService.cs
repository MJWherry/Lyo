namespace Lyo.FileStorage.Abstractions;

/// <summary>Diagnostic operations implemented by backends that expose object keys or paths (disk, blob, S3-compatible).</summary>
public interface IFileStorageDiagnosticsService
{
    /// <summary>Lists up to <paramref name="maxKeys"/> storage locations under optional <paramref name="prefix" /> relative to backend root/key prefix.</summary>
    Task<IReadOnlyList<string>> ListStorageKeysAsync(string? prefix = null, int maxKeys = 1000, CancellationToken ct = default);
}
