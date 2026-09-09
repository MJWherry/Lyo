namespace Lyo.Metrics.Models;

/// <summary>Lightweight struct returned by StartTimer. <see cref="NullMetrics" /> returns default for zero allocation; real implementations wrap a <see cref="Timer" />.</summary>
public readonly struct MetricsTimer : IDisposable
{
    private readonly Timer? _inner;

    /// <summary>Wraps the given <see cref="Timer" />, or default for a no-op (<see cref="NullMetrics" />).</summary>
    public MetricsTimer(Timer? inner) => _inner = inner;

    /// <summary>Elapsed time so far (<see cref="TimeSpan.Zero" /> when using <see cref="NullMetrics" />).</summary>
    public TimeSpan Elapsed => _inner?.Elapsed ?? TimeSpan.Zero;

    /// <summary>True when the timer has been disposed (also true when using <see cref="NullMetrics" />).</summary>
    public bool IsDisposed => _inner == null || _inner.IsDisposed;

    /// <summary>Writes elapsed time without disposing. No-op when using <see cref="NullMetrics" />.</summary>
    public void Record() => _inner?.Record();

    /// <summary>Restarts the timer from zero. No-op when using <see cref="NullMetrics" />.</summary>
    public void Restart() => _inner?.Restart();

    /// <inheritdoc />
    public void Dispose() => _inner?.Dispose();
}