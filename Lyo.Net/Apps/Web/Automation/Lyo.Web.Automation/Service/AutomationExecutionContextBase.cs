using Lyo.Web.Automation.Logging;
using Microsoft.Extensions.Logging;

namespace Lyo.Web.Automation.Service;

/// <summary>
/// Per-session scratch directories and their lifetime, shared by every browser engine. Disposing the context tears down the session log and deletes
/// <see cref="SessionDirectory" />, so an engine-specific session type only has to describe what it puts in those directories.
/// </summary>
/// <remarks>
/// <para>Dispose is idempotent and safe to call from both the synchronous and asynchronous paths; whichever runs first does the cleanup.</para>
/// </remarks>
public abstract class AutomationExecutionContextBase : IDisposable, IAsyncDisposable
{
    private int _disposed;

    /// <summary>Correlation id for logging and metrics.</summary>
    public Guid SessionId { get; init; }

    /// <summary>
    /// Per-session root: <c>{ServiceRootDirectory}/session-{SessionId:N}</c>. Holds <c>browser-profile/</c>, <c>artifacts/</c>, <c>downloads/</c> and acts as the root for
    /// plan-run logs, snapshots and variables. Deleted on dispose.
    /// </summary>
    public string SessionDirectory { get; init; } = null!;

    /// <summary>Browser user-data or profile directory when resolved.</summary>
    public string? BrowserUserDataDirectory { get; init; }

    /// <summary>Download directory handed to the browser.</summary>
    public string? DownloadDirectory { get; init; }

    /// <summary>Artifacts directory for engine diagnostics such as HAR files, traces, or driver logs.</summary>
    public string? ArtifactsDirectory { get; init; }

    /// <summary>Per-session file logger provider; writes to <c>{SessionDirectory}/session.log</c>. Disposed with this context.</summary>
    public SessionFileLoggerProvider? LoggerProvider { get; init; }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        Dispose();
        return default;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        LoggerProvider?.Dispose();
        TryDeleteSessionDirectory();
    }

    /// <summary>
    /// Returns a logger that fans output to both <paramref name="baseLogger" /> and the session log file, so an engine's own logging displays up in the per-session file without
    /// the
    /// engine knowing the file exists. Falls back to <paramref name="baseLogger" /> when no provider is set.
    /// </summary>
    /// <typeparam name="TCategory">Category type the session-file logger is created under.</typeparam>
    /// <param name="baseLogger">The ambient logger to keep writing to.</param>
    protected ILogger<TCategory> BuildLogger<TCategory>(ILogger<TCategory> baseLogger)
        => LoggerProvider != null ? new CompositeLogger<TCategory>(baseLogger, LoggerProvider.CreateLogger(typeof(TCategory).Name)) : baseLogger;

    /// <summary>
    /// Yields a logger that fans output to both <paramref name="baseLogger" /> and the session log file, for engines that hold a non-generic <see cref="ILogger" />. Falls back to
    /// <paramref name="baseLogger" /> when no provider is set.
    /// </summary>
    /// <typeparam name="TCategory">Category type the session-file logger is created under.</typeparam>
    /// <param name="baseLogger">The ambient logger to keep writing to.</param>
    protected ILogger BuildUntypedLogger<TCategory>(ILogger baseLogger)
        => LoggerProvider != null ? new CompositeLogger<TCategory>(baseLogger, LoggerProvider.CreateLogger(typeof(TCategory).Name)) : baseLogger;

    private void TryDeleteSessionDirectory()
    {
        if (string.IsNullOrWhiteSpace(SessionDirectory))
            return;

        try {
            if (Directory.Exists(SessionDirectory))
                Directory.Delete(SessionDirectory, true);
        }
        catch {
            // Best-effort: a browser process may still hold a handle, and the OS temp sweeper will get it later.
        }
    }
}
