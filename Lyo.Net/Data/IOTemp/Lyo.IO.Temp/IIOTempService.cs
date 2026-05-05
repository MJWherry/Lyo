using Lyo.IO.Temp.Models;

namespace Lyo.IO.Temp;

/// <summary>
/// Coordinates temporary file and directory storage under a unique <see cref="ServiceDirectory" />: session creation, keyed session reuse, one-off paths, and age-based cleanup.
/// </summary>
/// <remarks>
/// <para>
/// Disposing the service attempts to delete the entire service directory. Dispose open <see cref="IIOTempSession" /> instances first so their directories are not in use.
/// </para>
/// </remarks>
// ReSharper disable once InconsistentNaming
public interface IIOTempService : IDisposable
{
    /// <summary>Absolute path to the per-instance directory this service owns (under the configured temp root).</summary>
    string ServiceDirectory { get; }

    /// <summary>Number of sessions currently tracked as active (created and not yet disposed).</summary>
    int ActiveSessionCount { get; }

    /// <summary>
    /// Creates a new session whose files and directories live under <see cref="ServiceDirectory" />. Session options inherit from <see cref="IOTempServiceOptions" /> unless
    /// overridden.
    /// </summary>
    /// <param name="options">Optional per-session overrides; merged with service defaults.</param>
    /// <returns>A new <see cref="IIOTempSession" />; dispose it to delete its directory.</returns>
    IIOTempSession CreateSession(IOTempSessionOptions? options = null);

    /// <summary>
    /// Returns the existing session for <paramref name="key" /> or creates a new one if none exists (or the previous one was disposed). When creating,
    /// <paramref name="options" /> are applied; if the session already exists the options are ignored. Useful for per-request / per-pipeline session pools.
    /// </summary>
    /// <param name="key">Non-empty pool key.</param>
    /// <param name="options">Applied only when a new session is created.</param>
    IIOTempSession GetOrCreateSession(string key, IOTempSessionOptions? options = null);

    /// <summary>Disposes and removes the keyed session registered under <paramref name="key" />. No-op if the key is not found.</summary>
    /// <param name="key">The same key passed to <see cref="GetOrCreateSession" />.</param>
    void ReleaseSession(string key);

    /// <summary>Returns aggregate statistics for this service instance.</summary>
    IOTempServiceStats GetStats();

    /// <summary>Creates an empty file under <see cref="ServiceDirectory" /> (not tied to a session). Returns the full path.</summary>
    /// <param name="name">Optional relative name; generated if null or whitespace.</param>
    string CreateFile(string? name = null);

    /// <summary>Writes <paramref name="data" /> to a new file under <see cref="ServiceDirectory" />.</summary>
    /// <param name="data">File contents.</param>
    /// <param name="name">Optional relative name; generated if null or whitespace.</param>
    string CreateFile(ReadOnlyMemory<byte> data, string? name = null);

    /// <summary>Copies <paramref name="data" /> into a new file under <see cref="ServiceDirectory" />. The stream must be readable.</summary>
    /// <param name="data">Readable stream whose length is used when enforced by the storage layer.</param>
    /// <param name="name">Optional relative name; generated if null or whitespace.</param>
    string CreateFile(Stream data, string? name = null);

    /// <summary>Creates a directory under <see cref="ServiceDirectory" /> and returns its full path.</summary>
    /// <param name="name">Optional relative name; generated if null or whitespace.</param>
    string CreateDirectory(string? name = null);

    /// <summary>
    /// Deletes files and immediate subdirectories under <see cref="ServiceDirectory" /> whose creation time is older than <see cref="IOTempServiceOptions.FileLifetime" />, or
    /// all ages if that option is unset (uses zero age cutoff). Active session directories are skipped.
    /// </summary>
    void Cleanup();

    /// <summary>Schedules <see cref="Cleanup()" /> on the thread pool using the same age threshold as the parameterless cleanup.</summary>
    /// <param name="ct">Cancellation token.</param>
    Task CleanupAsync(CancellationToken ct = default);

    /// <summary>Schedules cleanup of entries whose creation time is at or before UTC now minus <paramref name="olderThan" />.</summary>
    /// <param name="olderThan">Minimum age of entries to remove.</param>
    /// <param name="ct">Cancellation token.</param>
    Task CleanupAsync(TimeSpan olderThan, CancellationToken ct = default);
}
