using System.Diagnostics.CodeAnalysis;
using Lyo.Metrics.Models;

namespace Lyo.Metrics.Tests;

public class MetricsServiceTests : IDisposable
{
    private readonly MetricsService _metrics = new();

    public void Dispose() => _metrics.Dispose();

    [Fact]
    public void Constructor_WithDefaultOptions_CreatesService()
    {
        var service = new MetricsService();
        Assert.NotNull(service);
        Assert.Equal(0, service.TotalMetricsRecorded);
    }

    [Fact]
    public void Constructor_WithCustomOptions_CreatesService()
    {
        var options = new MetricsOptions { MaxEventQueueSize = 5000, MaxHistogramValues = 500, SamplingRate = 0.5 };
        var service = new MetricsService(options);
        Assert.NotNull(service);
    }

    [Fact]
    public void IncrementCounter_WithDefaultValue_IncrementsByOne()
    {
        _metrics.IncrementCounter("test.counter");
        var value = _metrics.GetCounterValue("test.counter");
        Assert.Equal(1, value);
    }

    [Fact]
    public void IncrementCounter_WithCustomValue_IncrementsByValue()
    {
        _metrics.IncrementCounter("test.counter", 5);
        var value = _metrics.GetCounterValue("test.counter");
        Assert.Equal(5, value);
    }

    [Fact]
    public void IncrementCounter_MultipleTimes_Accumulates()
    {
        _metrics.IncrementCounter("test.counter", 3);
        _metrics.IncrementCounter("test.counter", 7);
        var value = _metrics.GetCounterValue("test.counter");
        Assert.Equal(10, value);
    }

    [Fact]
    public void DecrementCounter_DecrementsValue()
    {
        _metrics.IncrementCounter("test.counter", 10);
        _metrics.DecrementCounter("test.counter", 3);
        var value = _metrics.GetCounterValue("test.counter");
        Assert.Equal(7, value);
    }

    [Fact]
    public void IncrementCounter_WithTags_CreatesSeparateCounter()
    {
        _metrics.IncrementCounter("test.counter", 1, [("env", "prod")]);
        _metrics.IncrementCounter("test.counter", 1, [("env", "dev")]);
        var prodValue = _metrics.GetCounterValue("test.counter", [("env", "prod")]);
        var devValue = _metrics.GetCounterValue("test.counter", [("env", "dev")]);
        Assert.Equal(1, prodValue);
        Assert.Equal(1, devValue);
    }

    [Fact]
    public void RecordGauge_SetsValue()
    {
        _metrics.RecordGauge("test.gauge", 42.5);
        var value = _metrics.GetGaugeValue("test.gauge");
        Assert.Equal(42.5, value);
    }

    [Fact]
    public void RecordGauge_OverwritesPreviousValue()
    {
        _metrics.RecordGauge("test.gauge", 10.0);
        _metrics.RecordGauge("test.gauge", 20.0);
        var value = _metrics.GetGaugeValue("test.gauge");
        Assert.Equal(20.0, value);
    }

    [Fact]
    public void RecordHistogram_RecordsValue()
    {
        _metrics.RecordHistogram("test.histogram", 100.0);
        var histogram = _metrics.GetHistogram("test.histogram");
        Assert.NotNull(histogram);
        Assert.Single(histogram.Values);
        Assert.Equal(100.0, histogram.Values[0]);
    }

    [Fact]
    public void RecordHistogram_MultipleValues_Accumulates()
    {
        _metrics.RecordHistogram("test.histogram", 10.0);
        _metrics.RecordHistogram("test.histogram", 20.0);
        _metrics.RecordHistogram("test.histogram", 30.0);
        var histogram = _metrics.GetHistogram("test.histogram");
        Assert.NotNull(histogram);
        Assert.Equal(3, histogram.Count);
        Assert.Equal(10.0, histogram.Min);
        Assert.Equal(30.0, histogram.Max);
        Assert.Equal(20.0, histogram.Average);
        Assert.Equal(60.0, histogram.Sum);
    }

    [Fact]
    public void RecordTiming_RecordsDuration()
    {
        var duration = TimeSpan.FromMilliseconds(150);
        _metrics.RecordTiming("test.timing", duration);
        var histogram = _metrics.GetHistogram("test.timing");
        Assert.NotNull(histogram);
        Assert.Single(histogram.Values);
        Assert.Equal(150.0, histogram.Values[0]);
    }

    [Fact]
    public void StartTimer_RecordsDurationOnDispose()
    {
        using (var _ = _metrics.StartTimer("test.timer"))
            Thread.Sleep(50); // Small delay to ensure timing is recorded

        var histogram = _metrics.GetHistogram("test.timer");
        Assert.NotNull(histogram);
        Assert.Single(histogram.Values);
        Assert.True(histogram.Values[0] >= 50);
    }

    [Fact]
    public void RecordError_IncrementsErrorCounter()
    {
        var exception = new InvalidOperationException("Test error");
        _metrics.RecordError("test.operation", exception);

        // RecordError adds error_type and error_message tags automatically
        (string, string)[] errorTags = [("error_type", "InvalidOperationException"), ("error_message", "Test error")];
        var errorCount = _metrics.GetCounterValue("test.operation.errors", errorTags);
        Assert.Equal(1, errorCount);
    }

    [Fact]
    public void RecordError_WithTags_IncludesErrorInfo()
    {
        // Use a custom exception with a known message to avoid platform-specific message formatting
        var exception = new InvalidOperationException("Test error message");
        _metrics.RecordError("test.operation", exception, [("component", "api")]);

        // RecordError adds error_type and error_message tags automatically
        var errorTags = new[] { ("component", "api"), ("error_type", "InvalidOperationException"), ("error_message", "Test error message") };
        var errorCount = _metrics.GetCounterValue("test.operation.errors", errorTags);
        Assert.Equal(1, errorCount);
    }

    [Fact]
    public void GetCounterValue_NonExistent_ReturnsZero()
    {
        var value = _metrics.GetCounterValue("nonexistent");
        Assert.Equal(0, value);
    }

    [Fact]
    public void GetGaugeValue_NonExistent_ReturnsNull()
    {
        var value = _metrics.GetGaugeValue("nonexistent");
        Assert.Null(value);
    }

    [Fact]
    public void GetHistogram_NonExistent_ReturnsNull()
    {
        var histogram = _metrics.GetHistogram("nonexistent");
        Assert.Null(histogram);
    }

    [Fact]
    public void Clear_RemovesAllMetrics()
    {
        _metrics.IncrementCounter("test.counter");
        _metrics.RecordGauge("test.gauge", 10.0);
        _metrics.RecordHistogram("test.histogram", 5.0);
        _metrics.Clear();
        Assert.Equal(0, _metrics.GetCounterValue("test.counter"));
        Assert.Null(_metrics.GetGaugeValue("test.gauge"));
        Assert.Null(_metrics.GetHistogram("test.histogram"));
        Assert.Equal(0, _metrics.TotalMetricsRecorded);
    }

    [Fact]
    public void IncrementCounter_WithNullName_ThrowsException() => Assert.Throws<ArgumentNullException>(() => _metrics.IncrementCounter(null!));

    [Fact]
    public void IncrementCounter_WithEmptyName_ThrowsException() => Assert.Throws<ArgumentException>(() => _metrics.IncrementCounter(""));

    [Fact]
    public void RecordGauge_WithNullValue_ThrowsException() => Assert.Throws<ArgumentNullException>(() => _metrics.RecordGauge("test.gauge", null!));

    [Fact]
    public void RecordHistogram_WithNullValue_ThrowsException() => Assert.Throws<ArgumentNullException>(() => _metrics.RecordHistogram("test.histogram", null!));

    [Fact]
    public void IncrementCounter_WithDifferentNumericTypes_Works()
    {
        _metrics.IncrementCounter("test.int", 10);
        _metrics.IncrementCounter("test.long", 20L);
        _metrics.IncrementCounter("test.double", 30.0);
        _metrics.IncrementCounter("test.decimal", 40m);
        Assert.Equal(10, _metrics.GetCounterValue("test.int"));
        Assert.Equal(20, _metrics.GetCounterValue("test.long"));
        Assert.Equal(30, _metrics.GetCounterValue("test.double"));
        Assert.Equal(40, _metrics.GetCounterValue("test.decimal"));
    }

    [Fact]
    public void TotalMetricsRecorded_IncrementsCorrectly()
    {
        var initial = _metrics.TotalMetricsRecorded;
        _metrics.IncrementCounter("test1");
        _metrics.RecordGauge("test2", 10.0);
        _metrics.RecordHistogram("test3", 5.0);
        Assert.True(_metrics.TotalMetricsRecorded > initial);
        Assert.True(_metrics.TotalMetricsRecorded >= 3);
    }

    [Fact]
    public void GetEvents_ReturnsRecentEvents()
    {
        _metrics.IncrementCounter("test.event1");
        _metrics.IncrementCounter("test.event2");
        _metrics.IncrementCounter("test.event3");
        var events = _metrics.GetEvents(10).ToList();
        Assert.True(events.Count >= 3);
    }

    [Fact]
    public void GetEvents_WithMaxCount_LimitsResults()
    {
        for (var i = 0; i < 20; i++)
            _metrics.IncrementCounter($"test.event{i}");

        var events = _metrics.GetEvents(10).ToList();
        Assert.True(events.Count <= 10);
    }

    [Fact]
    public void Export_CreatesSnapshot()
    {
        _metrics.IncrementCounter("test.counter", 5);
        _metrics.RecordGauge("test.gauge", 10.0);
        _metrics.RecordHistogram("test.histogram", 15.0);
        var snapshot = _metrics.Export();
        Assert.NotNull(snapshot);
        Assert.True(snapshot.Counters.Count > 0);
        Assert.True(snapshot.Gauges.Count > 0);
        Assert.True(snapshot.Histograms.Count > 0);
        Assert.True(snapshot.TotalMetricsRecorded > 0);
        Assert.True(snapshot.ExportTime <= DateTime.UtcNow);
    }

    [Fact]
    public void Dispose_CleansUpResources()
    {
        var service = new MetricsService();
        service.IncrementCounter("test");
        service.Dispose();

        // Should not throw
        service.Dispose();
    }

    [Fact]
    [SuppressMessage("Usage", "xUnit1031:Do not use blocking task operations in test method")]
    public void ConcurrentAccess_IsThreadSafe()
    {
        var tasks = new List<Task>();
        for (var i = 0; i < 10; i++) {
            var threadId = i;
            tasks.Add(
                Task.Run(
                    () => {
                        for (var j = 0; j < 100; j++)
                            _metrics.IncrementCounter($"counter.{threadId}", 1);
                    }, TestContext.Current.CancellationToken));
        }

        Task.WaitAll(tasks.ToArray(), TestContext.Current.CancellationToken);

        // Verify all counters were incremented correctly
        for (var i = 0; i < 10; i++) {
            var value = _metrics.GetCounterValue($"counter.{i}");
            Assert.Equal(100, value);
        }
    }

    [Fact]
    public void Histogram_RespectsMaxValuesLimit()
    {
        var options = new MetricsOptions { MaxHistogramValues = 5 };
        var service = new MetricsService(options);
        for (var i = 0; i < 10; i++)
            service.RecordHistogram("test.histogram", i);

        var histogram = service.GetHistogram("test.histogram");
        Assert.NotNull(histogram);
        Assert.Equal(5, histogram.Count);
        // Should contain the last 5 values (5, 6, 7, 8, 9)
        Assert.True(histogram.Values.All(v => v >= 5));
    }

    [Fact]
    public void TagValidation_SanitizesInvalidCharacters()
    {
        var options = new MetricsOptions { ValidateTags = true };
        var service = new MetricsService(options);
        var tags = new[] { ("key|with|pipes", "value=with=equals"), ("normal_key", "normal_value") };
        service.IncrementCounter("test", 1, tags);

        // Tags are sanitized, so we need to query with sanitized tags
        // Pipes are removed from keys, equals are replaced with underscores in values
        var sanitizedTags = new[] { ("keywithpipes", "value_with_equals"), ("normal_key", "normal_value") }; // Pipes removed from key, equals replaced with underscores in value
        var value = service.GetCounterValue("test", sanitizedTags);
        Assert.Equal(1, value);
    }

    [Fact]
    public void SamplingRate_ReducesRecordedMetrics()
    {
        var options = new MetricsOptions { SamplingRate = 0.1 }; // 10% sampling
        var service = new MetricsService(options);
        for (var i = 0; i < 100; i++)
            service.IncrementCounter("test.sampled");

        // With 10% sampling, we should have roughly 10 +/- some variance
        var value = service.GetCounterValue("test.sampled");
        Assert.True(value < 100); // Should be less than 100 due to sampling
        Assert.True(value > 0); // But should have some values
    }

    [Fact]
    public void OverflowProtection_HandlesLargeValues()
    {
        _metrics.IncrementCounter("test.overflow", long.MaxValue - 10);
        _metrics.IncrementCounter("test.overflow", 20); // Should trigger overflow
        var value = _metrics.GetCounterValue("test.overflow");
        // Should be clamped to max value
        Assert.Equal(long.MaxValue, value);
    }
}