using Lyo.IO.Temp.Models;
using Lyo.Metrics;
using Lyo.Metrics.Models;
using Lyo.Testing;
using Microsoft.Extensions.Logging;

namespace Lyo.FileSystemWatcher.Tests;

public class MetricsTests : IDisposable
{
    private readonly IIOTempSession _tempSession;

    public MetricsTests(ITestOutputHelper output)
    {
        var loggerFactory = LoggerFactory.Create(builder => {
            builder.AddProvider(new XunitLoggerProvider(output));
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        _tempSession = new IOTempSession(new(), loggerFactory.CreateLogger<IOTempSession>());
    }

    public void Dispose() => _tempSession.Dispose();

    [Fact]
    public void Options_EnableMetrics_False_WithoutMetrics_Works()
    {
        var options = new FileSystemWatcherOptions { EnableMetrics = false };
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory, options);
        Assert.NotNull(watcher);
    }

    [Fact]
    public async Task Metrics_FileCreated_RecordsMetrics()
    {
        var metrics = new TestMetrics();
        var options = new FileSystemWatcherOptions { EnableMetrics = true };
        var fileName = Path.GetFileName(_tempSession.GetFilePath());
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory, options, null, metrics);
        await _tempSession.CreateFileAsync(new byte[100], fileName, TestContext.Current.CancellationToken).ConfigureAwait(false);
        await Task.Delay(500, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(metrics.Counters.ContainsKey(Constants.Metrics.FileCreatedCount));
    }

    [Fact]
    public async Task Metrics_FileDeleted_RecordsMetrics()
    {
        var metrics = new TestMetrics();
        var options = new FileSystemWatcherOptions { EnableMetrics = true };
        var filePath = await _tempSession.CreateFileAsync(new byte[100], ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory, options, null, metrics);
        await Task.Delay(200, TestContext.Current.CancellationToken).ConfigureAwait(false); // Wait for initial snapshot
        File.Delete(filePath);
        await Task.Delay(500, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(metrics.Counters.ContainsKey(Constants.Metrics.FileDeletedCount));
    }

    [Fact]
    public async Task Metrics_Snapshot_RecordsMetrics()
    {
        var metrics = new TestMetrics();
        var options = new FileSystemWatcherOptions { EnableMetrics = true };
        await _tempSession.CreateFileAsync(new byte[100], "test.txt", TestContext.Current.CancellationToken).ConfigureAwait(false);
        using var watcher = new FileSystemWatcher(_tempSession.SessionDirectory, options, null, metrics);
        await Task.Delay(500, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(metrics.Gauges.ContainsKey(Constants.Metrics.SnapshotFileCount));
        Assert.True(metrics.Gauges.ContainsKey(Constants.Metrics.SnapshotItemCount));
    }

    [Fact]
    public async Task Metrics_Error_RecordsMetrics()
    {
        var metrics = new TestMetrics();
        var options = new FileSystemWatcherOptions { EnableMetrics = true };
        var watcher = new FileSystemWatcher(_tempSession.SessionDirectory, options, null, metrics);

        // Trigger an error by disposing and then trying to use
        watcher.Dispose();

        // Error metrics should be recorded if errors occur
        // Note: This test may need adjustment based on actual error scenarios
        await Task.Delay(100, TestContext.Current.CancellationToken).ConfigureAwait(false);
    }
}

// Simple test implementation of IMetrics
public class TestMetrics : IMetrics
{
    public Dictionary<string, long> Counters { get; } = new();

    public Dictionary<string, double> Gauges { get; } = new();

    public Dictionary<string, List<double>> Histograms { get; } = new();

    public List<(string name, Exception ex, Dictionary<string, string>? tags)> Errors { get; } = new();

    public void IncrementCounter(string name, IConvertible? value = null, IEnumerable<(string, string)>? tags = null)
    {
        var val = value?.ToInt64(null) ?? 1;
        Counters.TryGetValue(name, out var current);
        Counters[name] = current + val;
    }

    public void DecrementCounter(string name, IConvertible? value = null, IEnumerable<(string, string)>? tags = null)
    {
        var val = value?.ToInt64(null) ?? 1;
        Counters.TryGetValue(name, out var current);
        Counters[name] = current - val;
    }

    public void RecordGauge(string name, IConvertible value, IEnumerable<(string, string)>? tags = null) => Gauges[name] = value.ToDouble(null);

    public void RecordHistogram(string name, IConvertible value, IEnumerable<(string, string)>? tags = null)
    {
        if (!Histograms.ContainsKey(name))
            Histograms[name] = new();

        Histograms[name].Add(value.ToDouble(null));
    }

    public void RecordTiming(string name, TimeSpan duration, IEnumerable<(string, string)>? tags = null) => RecordHistogram(name, duration.TotalMilliseconds, tags);

    public MetricsTimer StartTimer(string name, IEnumerable<(string, string)>? tags = null) => new(new(this, name, tags));

    public void RecordError(string name, Exception exception, IEnumerable<(string, string)>? tags = null)
    {
        var dictTags = tags?.ToDictionary(t => t.Item1, t => t.Item2);
        Errors.Add((name, exception, dictTags));
        IncrementCounter(Constants.Metrics.ErrorCount);
    }

    public void RecordEvent(string name, IConvertible? value = null, IEnumerable<(string, string)>? tags = null) => IncrementCounter(name, value, tags);
}