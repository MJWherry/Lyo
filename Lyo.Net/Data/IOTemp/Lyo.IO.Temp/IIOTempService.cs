using Lyo.IO.Temp.Models;

namespace Lyo.IO.Temp;

// ReSharper disable once InconsistentNaming
public interface IIOTempService : IDisposable
{
    /// <summary>The per-instance subdirectory under the root that this service owns.</summary>
    string ServiceDirectory { get; }

    int ActiveSessionCount { get; }

    IIOTempSession CreateSession(IOTempSessionOptions? options = null);

    /// <summary>
    /// Returns the existing session for <paramref name="key" /> or creates a new one if none exists (or the previous one was disposed). When creating,
    /// <paramref name="options" /> are applied; if the session already exists the options are ignored. Useful for per-request / per-pipeline session pools.
    /// </summary>
    IIOTempSession GetOrCreateSession(string key, IOTempSessionOptions? options = null);

    /// <summary>Disposes and removes the keyed session registered under <paramref name="key" />. No-op if the key is not found.</summary>
    void ReleaseSession(string key);

    /// <summary>Returns aggregate statistics for this service instance.</summary>
    IOTempServiceStats GetStats();

    string CreateFile(string? name = null);

    string CreateFile(ReadOnlyMemory<byte> data, string? name = null);

    string CreateFile(Stream data, string? name = null);

    string CreateDirectory(string? name = null);

    void Cleanup();

    Task CleanupAsync(CancellationToken ct = default);

    Task CleanupAsync(TimeSpan olderThan, CancellationToken ct = default);
}