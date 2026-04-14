namespace Lyo.Metrics.Models;

/// <summary>Lightweight struct returned by StartTimer. NullMetrics returns default for zero allocation; real implementations wrap a Timer.</summary>
public readonly struct MetricsTimer : IDisposable
{
    private readonly Timer? _inner;

    /// <summary>Creates a MetricsTimer wrapping the given Timer, or default for no-op (NullMetrics).</summary>
    public MetricsTimer(Timer? inner) => _inner = inner;

    /// <summary>Gets the elapsed time so far (TimeSpan.Zero when using NullMetrics).</summary>
    public TimeSpan Elapsed => _inner?.Elapsed ?? TimeSpan.Zero;

    /// <summary>Gets whether the timer has been disposed (true when using NullMetrics).</summary>
    public bool IsDisposed => _inner == null || _inner.IsDisposed;

    /// <summary>Records the elapsed time without disposing. No-op when using NullMetrics.</summary>
    public void Record() => _inner?.Record();

    /// <summary>Restarts the timer from zero. No-op when using NullMetrics.</summary>
    public void Restart() => _inner?.Restart();

    /// <inheritdoc />
    public void Dispose() => _inner?.Dispose();
}