using System.Diagnostics;
using Lyo.Exceptions;

namespace Lyo.Metrics.Models;

/// <summary>A timer that writes elapsed time when disposed. Use with <c>using</c> for automatic timing.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class Timer : IDisposable
{
    private readonly IMetrics _metrics;

    private readonly string _name;

    private readonly Stopwatch _stopwatch;

    private readonly IEnumerable<(string, string)>? _tags;

    /// <summary>Elapsed time so far, without stopping the timer.</summary>
    public TimeSpan Elapsed => _stopwatch.Elapsed;

    /// <summary>True when the timer has been disposed (stopped).</summary>
    public bool IsDisposed { get; private set; }

    /// <summary>Starts a timer that writes to <paramref name="metrics" />.</summary>
    /// <param name="metrics">Metrics service to write to</param>
    /// <param name="name">Timing metric name</param>
    /// <param name="tags">Optional tags/labels</param>
    public Timer(IMetrics metrics, string name, IEnumerable<(string, string)>? tags)
    {
        _metrics = ArgumentHelpers.ThrowIfNullReturn(metrics);
        _name = ArgumentHelpers.ThrowIfNullReturn(name);
        _tags = tags;
        _stopwatch = Stopwatch.StartNew();
    }

    /// <summary>Stops the timer and writes the elapsed time.</summary>
    public void Dispose()
    {
        if (IsDisposed)
            return;

        _stopwatch.Stop();
        _metrics.RecordTiming(_name, _stopwatch.Elapsed, _tags);
        IsDisposed = true;
    }

    /// <summary>Writes the elapsed time without disposing, so the timer can keep running.</summary>
    public void Record()
    {
        if (IsDisposed)
            return;

        _metrics.RecordTiming(_name, _stopwatch.Elapsed, _tags);
    }

    /// <summary>Restarts the timer from zero.</summary>
    public void Restart() => _stopwatch.Restart();

    public override string ToString() => $"Name: {_name}, Elapsed: {_stopwatch.Elapsed:g}, IsDisposed: {IsDisposed}";
}