namespace Lyo.Metrics.Tests;

public class TimerTests : IDisposable
{
    private readonly MetricsService _metrics = new();

    public void Dispose() => _metrics.Dispose();

    [Fact]
    public void Timer_RecordsDurationOnDispose()
    {
        using (var _ = _metrics.StartTimer("test.timer"))
            Thread.Sleep(50);

        var histogram = _metrics.GetHistogram("test.timer");
        Assert.NotNull(histogram);
        Assert.Single(histogram.Values);
        Assert.True(histogram.Values[0] >= 50);
    }

    [Fact]
    public void Timer_Elapsed_ReturnsCurrentDuration()
    {
        using var timer = _metrics.StartTimer("test.timer");
        Thread.Sleep(50);
        var elapsed = timer.Elapsed;
        Assert.True(elapsed.TotalMilliseconds >= 50);
    }

    [Fact]
    public void Timer_Record_RecordsWithoutDisposing()
    {
        var timer = _metrics.StartTimer("test.timer");
        Thread.Sleep(50);
        timer.Record();
        var histogram = _metrics.GetHistogram("test.timer");
        Assert.NotNull(histogram);
        Assert.Single(histogram.Values);

        // Should still be able to record again
        Thread.Sleep(50);
        timer.Record();
        histogram = _metrics.GetHistogram("test.timer");
        Assert.Equal(2, histogram!.Count);
        timer.Dispose();
    }

    [Fact]
    public void Timer_Restart_ResetsElapsed()
    {
        using var timer = _metrics.StartTimer("test.timer");
        Thread.Sleep(50);
        var elapsed1 = timer.Elapsed;
        timer.Restart();
        Thread.Sleep(30);
        var elapsed2 = timer.Elapsed;
        Assert.True(elapsed1.TotalMilliseconds >= 50);
        Assert.True(elapsed2.TotalMilliseconds >= 30);
        Assert.True(elapsed2.TotalMilliseconds < elapsed1.TotalMilliseconds);
    }

    [Fact]
    public void Timer_IsDisposed_ReturnsFalseBeforeDispose()
    {
        var timer = _metrics.StartTimer("test.timer");
        Assert.False(timer.IsDisposed);
        timer.Dispose();
        Assert.True(timer.IsDisposed);
    }

    [Fact]
    public void Timer_DisposeTwice_DoesNotThrow()
    {
        var timer = _metrics.StartTimer("test.timer");
        timer.Dispose();
        timer.Dispose(); // Should not throw
    }

    [Fact]
    public void Timer_WithTags_IncludesTags()
    {
        var tags = new[] { ("env", "test") };
        using (var _ = _metrics.StartTimer("test.timer", tags))
            Thread.Sleep(10);

        var histogram = _metrics.GetHistogram("test.timer", tags);
        Assert.NotNull(histogram);
    }
}