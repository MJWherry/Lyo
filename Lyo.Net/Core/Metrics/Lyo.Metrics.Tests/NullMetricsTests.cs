namespace Lyo.Metrics.Tests;

public class NullMetricsTests
{
    [Fact]
    public void NullMetrics_DoesNotThrow()
    {
        var metrics = NullMetrics.Instance;
        metrics.IncrementCounter("test");
        metrics.DecrementCounter("test");
        metrics.RecordGauge("test", 10.0);
        metrics.RecordHistogram("test", 5.0);
        metrics.RecordTiming("test", TimeSpan.FromSeconds(1));
        metrics.RecordError("test", new("test"));
        metrics.RecordEvent("test");
        using (metrics.StartTimer("test")) { }
    }

    [Fact]
    public void NullMetrics_AcceptsNullValues()
    {
        var metrics = NullMetrics.Instance;
        metrics.IncrementCounter("test");
        metrics.RecordEvent("test");
    }

    [Fact]
    public void NullMetrics_Instance_IsSingleton()
    {
        var instance1 = NullMetrics.Instance;
        var instance2 = NullMetrics.Instance;
        Assert.Same(instance1, instance2);
    }
}