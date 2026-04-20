using Lyo.IO.Temp.Models;
using Lyo.Web.Automation.Selenium.Browser;

namespace Lyo.Web.Automation.Selenium.Service;

/// <summary>Per-<see cref="ISeleniumBrowserSession" /> paths and temp lifetime (not used for DI-scoped <see cref="LyoBrowser" /> without a session).</summary>
public sealed class SeleniumExecutionContext : IDisposable, IAsyncDisposable
{
    private int _disposed;

    /// <summary>Correlation id for logging and metrics.</summary>
    public Guid SessionId { get; init; }

    /// <summary>Chrome/Edge user-data-dir or Firefox profile directory, when resolved.</summary>
    public string? BrowserUserDataDirectory { get; init; }

    /// <summary>Browser download folder.</summary>
    public string? DownloadDirectory { get; init; }

    /// <summary>WebDriver logs, driver service logs, etc.</summary>
    public string? ArtifactsDirectory { get; init; }

    /// <summary>When non-null, disposed after the browser is torn down.</summary>
    public IIOTempSession? IoTempSession { get; init; }

    /// <summary>When <see cref="IoTempSession" /> was not used, this folder is deleted on dispose.</summary>
    public string? FallbackTempRoot { get; init; }

    /// <inheritdoc />
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        IoTempSession?.Dispose();
        TryDeleteFallback();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        if (IoTempSession != null)
            await IoTempSession.DisposeAsync().ConfigureAwait(false);
        else
            TryDeleteFallback();
    }

    private void TryDeleteFallback()
    {
        if (string.IsNullOrWhiteSpace(FallbackTempRoot))
            return;

        try {
            if (Directory.Exists(FallbackTempRoot))
                Directory.Delete(FallbackTempRoot, recursive: true);
        }
        catch {
            // best-effort cleanup
        }
    }
}
