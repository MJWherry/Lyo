using Microsoft.Extensions.Logging;

namespace Lyo.Desktop.Components.Tests;

public class LogSinkTests
{
    [Fact]
    public void Logger_WritesTheMessageToTheSink()
    {
        var sink = new LogSink();
        LogLine? line = null;
        sink.Written += text => line = text;
        var logger = new LogSinkLoggerFactory(new LogSinkLoggerProvider(sink)).CreateLogger("test");
        logger.LogInformation("Connected.");
        Assert.NotNull(line);
        Assert.Equal("Connected.", line.Message);
        Assert.Equal(LogLevel.Information, line.Level);
        Assert.Equal("test", line.Source);
    }

    [Fact]
    public void Logger_Warning_KeepsTheLevel()
    {
        var sink = new LogSink();
        LogLine? line = null;
        sink.Written += text => line = text;
        var logger = new LogSinkLoggerFactory(new LogSinkLoggerProvider(sink)).CreateLogger("test");
        logger.LogWarning("Disk full.");
        Assert.NotNull(line);
        Assert.Equal(LogLevel.Warning, line.Level);
        Assert.Equal("Disk full.", line.Message);
    }

    [Fact]
    public void Logger_Error_KeepsScopeContextAndException()
    {
        var sink = new LogSink();
        LogLine? line = null;
        sink.Written += text => line = text;
        var logger = new LogSinkLoggerFactory(new LogSinkLoggerProvider(sink)).CreateLogger("studio");
        InvalidOperationException error;
        try {
            throw new InvalidOperationException("Navigation failed");
        }
        catch (InvalidOperationException ex) {
            error = ex;
        }

        using (logger.BeginScope(new Dictionary<string, object?> { ["automation_run_id"] = "run-1" }))
            logger.LogError(error, "Plan failed {Url}", "https://example.com/search");

        Assert.NotNull(line);
        Assert.Equal(LogLevel.Error, line.Level);
        Assert.Contains(line.Context, field => field.Name == "automation_run_id" && field.Value == "run-1");
        Assert.Contains(line.Context, field => field.Name == "Url" && field.Value == "https://example.com/search");
        Assert.DoesNotContain(line.Context, field => field.Name == "{OriginalFormat}");
        Assert.Contains("InvalidOperationException", line.ExceptionText);
        Assert.Contains("Navigation failed", line.ExceptionText);
        Assert.Contains("at ", line.ExceptionText, StringComparison.Ordinal);
    }

    [Fact]
    public void Logger_AppendsExceptionMessageWhenMissing()
    {
        var sink = new LogSink();
        LogLine? line = null;
        sink.Written += text => line = text;
        var logger = new LogSinkLoggerFactory(new LogSinkLoggerProvider(sink)).CreateLogger("test");
        logger.LogError(new InvalidOperationException("boom"), "Failed.");
        Assert.NotNull(line);
        Assert.Equal("Failed. boom", line.Message);
        logger.LogError(new InvalidOperationException("boom"), "Failed. boom");
        Assert.Equal("Failed. boom", line.Message);
    }

    [Fact]
    public void Logger_BelowMinimumLevel_IsDropped()
    {
        var sink = new LogSink();
        var count = 0;
        sink.Written += _ => count++;
        var logger = new LogSinkLoggerFactory(new LogSinkLoggerProvider(sink)).CreateLogger("test");
        logger.LogDebug("hidden");
        Assert.Equal(0, count);
        Assert.Empty(sink.Snapshot());
    }

    [Fact]
    public void Write_SkipsBlankLines()
    {
        var sink = new LogSink();
        var count = 0;
        sink.Written += _ => count++;
        sink.Write("  ");
        sink.Write(null);
        Assert.Equal(0, count);
    }

    [Fact]
    public void Write_StampsUtcTimestamp()
    {
        var sink = new LogSink();
        var before = DateTimeOffset.UtcNow;
        sink.Write("Connected.");
        var line = Assert.Single(sink.Snapshot());
        Assert.Equal(LogLevel.Information, line.Level);
        Assert.Equal("Connected.", line.Message);
        Assert.Equal(TimeSpan.Zero, line.Timestamp.Offset);
        Assert.InRange(line.Timestamp, before.AddSeconds(-1), DateTimeOffset.UtcNow.AddSeconds(1));
    }

    [Fact]
    public void Snapshot_DropsOldestPastCapacity()
    {
        var sink = new LogSink(2);
        sink.Write("one");
        sink.Write("two");
        sink.Write("three");
        var lines = sink.Snapshot();
        Assert.Equal(["two", "three"], lines.Select(line => line.Message));
    }

    [Fact]
    public void Clear_RemovesLines()
    {
        var sink = new LogSink();
        var cleared = false;
        sink.Cleared += () => cleared = true;
        sink.Write("hi");
        sink.Clear();
        Assert.Empty(sink.Snapshot());
        Assert.True(cleared);
    }
}
