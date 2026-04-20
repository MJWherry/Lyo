using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Lyo.IO.Temp.Models;

namespace Lyo.Web.Automation.Playwright.Service;

/// <summary>Per-session paths and temp lifetime when created from <see cref="IPlaywrightBrowserService.CreateSession" />.</summary>
public sealed class PlaywrightExecutionContext : IDisposable, IAsyncDisposable
{
    private int _disposed;

    /// <summary>Correlation id for logging and metrics.</summary>
    public Guid SessionId { get; set; }

    /// <summary>Browser user-data directory when resolved.</summary>
    public string? BrowserUserDataDirectory { get; set; }

    /// <summary>Download directory for the browser context.</summary>
    public string? DownloadDirectory { get; set; }

    /// <summary>Artifacts directory (HAR, traces).</summary>
    public string? ArtifactsDirectory { get; set; }

    /// <summary>When non-null, disposed after the browser is torn down.</summary>
    public IIOTempSession? IoTempSession { get; set; }

    /// <summary>When <see cref="IoTempSession" /> was not used, this folder is deleted on dispose.</summary>
    public string? FallbackTempRoot { get; set; }

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
