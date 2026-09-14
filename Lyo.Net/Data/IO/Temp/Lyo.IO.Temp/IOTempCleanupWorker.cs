using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
namespace Lyo.IO.Temp;

/// <summary>
/// Hosted service that calls <see cref="IIOTempService.Cleanup" /> on a timer to drop leftover temp files and folders past their lifetime. Register with
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

    public IOTempCleanupWorker(IIOTempService service, IOTempCleanupOptions? options = null, ILogger<IOTempCleanupWorker>? logger = null)
    {
        _service = service;
        _options = options ?? new IOTempCleanupOptions();
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
        // Skip if a previous run is still going when the interval fires again.
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