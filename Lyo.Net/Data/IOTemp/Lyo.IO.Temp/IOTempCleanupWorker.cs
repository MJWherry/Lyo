using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Lyo.IO.Temp;

/// <summary>
/// A background service that periodically calls <see cref="IIOTempService.Cleanup" /> to remove orphaned temp files and directories that have exceeded their lifetime. Register via
/// <see
///     cref="Extensions.AddIOTempServiceWithAutoCleanup(Microsoft.Extensions.DependencyInjection.IServiceCollection,System.Nullable{System.TimeSpan},System.Nullable{System.TimeSpan})" />
/// .
/// </summary>
public sealed class IOTempCleanupWorker : IHostedService, IDisposable
{
    private readonly ILogger<IOTempCleanupWorker> _logger;
    private readonly IOTempCleanupOptions _options;
    private readonly IIOTempService _service;
    private int _running;
    private Timer? _timer;

    public IOTempCleanupWorker(IIOTempService service, IOptions<IOTempCleanupOptions>? options = null, ILogger<IOTempCleanupWorker>? logger = null)
    {
        _service = service;
        _options = options?.Value ?? new IOTempCleanupOptions();
        _logger = logger ?? NullLogger<IOTempCleanupWorker>.Instance;
    }

    public void Dispose() => _timer?.Dispose();

    public Task StartAsync(CancellationToken ct)
    {
        _logger.LogInformation("IOTemp cleanup worker starting. InitialDelay: {InitialDelay}, Interval: {Interval}", _options.InitialDelay, _options.Interval);
        _timer = new(DoCleanup, null, _options.InitialDelay, _options.Interval);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct)
    {
        _logger.LogInformation("IOTemp cleanup worker stopping.");
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    private void DoCleanup(object? state)
    {
        // Guard against overlapping invocations if cleanup takes longer than the interval.
        if (Interlocked.CompareExchange(ref _running, 1, 0) != 0)
            return;

        try {
            _logger.LogDebug("Running scheduled IOTemp cleanup.");
            _service.Cleanup();
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error during scheduled IOTemp cleanup.");
        }
        finally {
            Interlocked.Exchange(ref _running, 0);
        }
    }
}