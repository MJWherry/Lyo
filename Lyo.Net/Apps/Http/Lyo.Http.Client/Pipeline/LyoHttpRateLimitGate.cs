using System.Threading.RateLimiting;
using Lyo.Exceptions;

namespace Lyo.Http.Client.Pipeline;

/// <summary>
/// Per-process waiter queue and concurrency cap for one named HTTP client. Handlers are transient; this object is a singleton so the caps survive pooling. The shared quota
/// itself lives on <c>IRateLimitService</c>.
/// </summary>
public sealed class LyoHttpRateLimitGate
{
    private readonly SemaphoreSlim? _queue;
    private readonly int _queueLimit;

    /// <summary>In-flight cap. Null when <see cref="LyoHttpRateLimitOptions.MaxConcurrent" /> is zero.</summary>
    public ConcurrencyLimiter? Concurrency { get; }

    /// <summary>Creates the queue and optional concurrency limiter from <paramref name="options" />.</summary>
    public LyoHttpRateLimitGate(LyoHttpRateLimitOptions options)
    {
        ArgumentHelpers.ThrowIfNull(options);
        _queueLimit = options.QueueLimit;
        if (options.QueueLimit > 0)
            _queue = new(options.QueueLimit, options.QueueLimit);

        if (options.MaxConcurrent > 0) {
            Concurrency = new(new() { PermitLimit = options.MaxConcurrent, QueueLimit = Math.Max(options.QueueLimit, options.MaxConcurrent) });
        }
    }

    /// <summary>Enters the waiter queue without waiting. False when the queue is already full.</summary>
    public async Task<bool> TryEnterAsync(CancellationToken ct)
    {
        if (_queueLimit <= 0 || _queue is null)
            return false;

        return await _queue.WaitAsync(0, ct).ConfigureAwait(false);
    }

    /// <summary>Leaves the waiter queue.</summary>
    public void Exit() => _queue?.Release();
}