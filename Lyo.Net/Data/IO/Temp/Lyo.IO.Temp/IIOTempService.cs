using Lyo.IO.Temp.Models;

namespace Lyo.IO.Temp;

/// <summary>
/// Owns a unique <see cref="ServiceDirectory" /> for temp files: sessions, keyed reuse, one-off paths, and age-based cleanup.
/// </summary>
/// <remarks>
/// <para>
/// Disposing the service tries to delete the whole service folder. Dispose open <see cref="IIOTempSession" /> instances first so those folders are not still in use.
/// </para>
/// </remarks>
// ReSharper disable once InconsistentNaming
public interface IIOTempService : IDisposable
{
    /// <summary>Absolute path of this instance's folder under the configured temp root.</summary>
    string ServiceDirectory { get; }

    /// <summary>How many sessions are created and not yet disposed.</summary>
    int ActiveSessionCount { get; }

    /// <summary>
    /// Fires after <see cref="CreateSession" /> or the first <see cref="GetOrCreateSession" /> for a key. The argument is the session directory.
    /// </summary>
    event Action<string>? SessionCreated;

    /// <summary>Fires when a tracked session is disposed, including <see cref="ReleaseSession" />. The argument is the session directory.</summary>
    event Action<string>? SessionDisposed;

    /// <summary>Fires when a one-off file is created under <see cref="ServiceDirectory" />. The argument is the absolute path.</summary>
    event Action<string>? FileCreated;

    /// <summary>Fires when a one-off directory is created under <see cref="ServiceDirectory" />. The argument is the absolute path.</summary>
    event Action<string>? DirectoryCreated;

    /// <summary>Fires once per file <see cref="Cleanup()" /> actually removes. The argument is the absolute path.</summary>
    event Action<string>? FileDeleted;

    /// <summary>Fires once per directory <see cref="Cleanup()" /> actually removes. The argument is the absolute path.</summary>
    event Action<string>? DirectoryDeleted;

    /// <summary>
    /// Opens a new session under <see cref="ServiceDirectory" />. Session options come from <see cref="IOTempServiceOptions" /> unless <paramref name="options" /> overrides
    /// them.
    /// </summary>
    /// <param name="options">Optional per-session overrides merged onto service defaults.</param>
    /// <returns>A new <see cref="IIOTempSession" />. Dispose it to delete its folder.</returns>
    IIOTempSession CreateSession(IOTempSessionOptions? options = null);

    /// <summary>
    /// Returns the session already stored for <paramref name="key" />, or creates one if missing (or if the previous session was disposed). <paramref name="options" /> apply
    /// only on create; an existing session ignores them. Typical use is a per-request or per-pipeline pool.
    /// </summary>
    /// <param name="key">Non-empty pool key.</param>
    /// <param name="options">Used only when a new session is created.</param>
    IIOTempSession GetOrCreateSession(string key, IOTempSessionOptions? options = null);

    /// <summary>Disposes and unregisters the keyed session for <paramref name="key" />. Does nothing if the key is unknown.</summary>
    /// <param name="key">The key previously passed to <see cref="GetOrCreateSession" />.</param>
    void ReleaseSession(string key);

    /// <summary>Point-in-time totals for this service instance.</summary>
    IOTempServiceStats GetStats();

    /// <summary>Creates an empty file under <see cref="ServiceDirectory" /> that is not part of a session. Returns the absolute path.</summary>
    /// <param name="name">Optional relative name; generated when null or whitespace.</param>
    string CreateFile(string? name = null);

    /// <summary>Writes <paramref name="data" /> into a new file under <see cref="ServiceDirectory" />.</summary>
    /// <param name="data">File contents.</param>
    /// <param name="name">Optional relative name; generated when null or whitespace.</param>
    string CreateFile(ReadOnlyMemory<byte> data, string? name = null);

    /// <summary>Copies a readable <paramref name="data" /> stream into a new file under <see cref="ServiceDirectory" />.</summary>
    /// <param name="data">Readable stream. Implementations may use its length when they enforce size limits.</param>
    /// <param name="name">Optional relative name; generated when null or whitespace.</param>
    string CreateFile(Stream data, string? name = null);

    /// <summary>Creates a directory under <see cref="ServiceDirectory" /> and returns its absolute path.</summary>
    /// <param name="name">Optional relative name; generated when null or whitespace.</param>
    string CreateDirectory(string? name = null);

    /// <summary>
    /// Removes files and immediate child directories under <see cref="ServiceDirectory" /> older than <see cref="IOTempServiceOptions.FileLifetime" />. If that option is unset,
    /// age is treated as zero (everything eligible). Directories belonging to active sessions are left alone.
    /// </summary>
    void Cleanup();

    /// <summary>Queues <see cref="Cleanup()" /> on the thread pool with the same age cutoff as the parameterless method.</summary>
    /// <param name="ct">Cancellation token.</param>
    Task CleanupAsync(CancellationToken ct = default);

    /// <summary>Queues cleanup of entries whose creation time is at or before UTC now minus <paramref name="olderThan" />.</summary>
    /// <param name="olderThan">Minimum age of entries to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    Task CleanupAsync(TimeSpan olderThan, CancellationToken ct = default);
}
