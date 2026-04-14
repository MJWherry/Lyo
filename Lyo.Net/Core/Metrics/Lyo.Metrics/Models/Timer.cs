using System.Diagnostics;

namespace Lyo.Metrics.Models;

/// <summary>A timer that automatically records the elapsed time when disposed. Use with a using statement for automatic timing.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class Timer : IDisposable
{
    private readonly IMetrics _metrics;

    private readonly string _name;

    private readonly Stopwatch _stopwatch;

    private readonly IEnumerable<(string, string)>? _tags;

    /// <summary>Gets the elapsed time so far without stopping the timer.</summary>
    public TimeSpan Elapsed => _stopwatch.Elapsed;

    /// <summary>Gets whether the timer has been disposed (stopped).</summary>
    public bool IsDisposed { get; private set; }

    /// <summary>Creates a new timer instance.</summary>
    /// <param name="metrics">The metrics service to record to</param>
    /// <param name="name">The name of the timing metric</param>
    /// <param name="tags">Optional tags/labels for the metric</param>
    public Timer(IMetrics metrics, string name, IEnumerable<(string, string)>? tags)
    {
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        _name = name ?? throw new ArgumentNullException(nameof(name));
        _tags = tags;
        _stopwatch = Stopwatch.StartNew();
    }

    /// <summary>Stops the timer and records the elapsed time.</summary>
    public void Dispose()
    {
        if (IsDisposed)
            return;

        _stopwatch.Stop();
        _metrics.RecordTiming(_name, _stopwatch.Elapsed, _tags);
        IsDisposed = true;
    }

    /// <summary>Stops the timer and records the elapsed time without disposing. Useful if you want to record the timing but continue using the timer.</summary>
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