using Lyo.Web.Automation.Logging;
using Lyo.Web.Automation.Selenium.Browser;
using Microsoft.Extensions.Logging;

namespace Lyo.Web.Automation.Selenium.Service;

/// <summary>Per-<see cref="ISeleniumBrowserSession" /> paths and temp lifetime.</summary>
public sealed class SeleniumExecutionContext : IDisposable, IAsyncDisposable
{
    private int _disposed;

    /// <summary>Correlation id for logging and metrics.</summary>
    public Guid SessionId { get; init; }

    /// <summary>
    /// Per-session root: <c>{ServiceRootDirectory}/session-{SessionId:N}</c>.
    /// Holds <c>browser-profile/</c>, <c>artifacts/</c>, <c>downloads/</c> and acts as
    /// the root for plan-run logs, snapshots and variables. Deleted on dispose.
    /// </summary>
    public string SessionDirectory { get; init; } = null!;

    /// <summary>Chrome/Edge user-data-dir or Firefox profile directory.</summary>
    public string? BrowserUserDataDirectory { get; init; }

    /// <summary>Browser download folder.</summary>
    public string? DownloadDirectory { get; init; }

    /// <summary>WebDriver logs, driver service logs, etc.</summary>
    public string? ArtifactsDirectory { get; init; }

    /// <summary>Per-session file logger provider; writes to <c>{SessionDirectory}/session.log</c>. Disposed with this context.</summary>
    internal SessionFileLoggerProvider? LoggerProvider { get; init; }

    /// <summary>
    /// Returns a logger that fans output to both <paramref name="baseLogger" /> and the session log file.
    /// Falls back to <paramref name="baseLogger" /> when no provider is set.
    /// </summary>
    internal ILogger BuildLogger(ILogger baseLogger)
        => LoggerProvider != null
            ? new CompositeLogger<SeleniumBrowser>(baseLogger, LoggerProvider.CreateLogger(nameof(SeleniumBrowser)))
            : baseLogger;

    /// <inheritdoc />
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        LoggerProvider?.Dispose();
        TryDeleteSessionDir();
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return default;

        LoggerProvider?.Dispose();
        TryDeleteSessionDir();
        return default;
    }

    private void TryDeleteSessionDir()
    {
        if (string.IsNullOrWhiteSpace(SessionDirectory))
            return;

        try {
            if (Directory.Exists(SessionDirectory))
                Directory.Delete(SessionDirectory, recursive: true);
        }
        catch {
            // best-effort cleanup
        }
    }
}
