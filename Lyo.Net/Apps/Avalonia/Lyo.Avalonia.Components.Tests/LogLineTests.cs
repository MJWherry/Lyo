using Microsoft.Extensions.Logging;

namespace Lyo.Desktop.Components.Tests;

public class LogLineTests
{
    private static readonly DateTimeOffset Stamp = new(2026, 10, 6, 22, 30, 0, TimeSpan.Zero);

    [Fact]
    public void Matches_MinLevelAndSearch()
    {
        var debug = new LogLine(Stamp, LogLevel.Debug, "detail", "src");
        var info = new LogLine(Stamp, LogLevel.Information, "Hello World", "Worker");
        Assert.False(LogLine.Matches(debug, LogLevel.Information, null));
        Assert.True(LogLine.Matches(info, LogLevel.Information, "hello"));
        Assert.True(LogLine.Matches(info, LogLevel.Information, "work"));
        Assert.True(LogLine.Matches(info, LogLevel.Information, "  "));
        Assert.False(LogLine.Matches(info, LogLevel.Information, "missing"));
    }

    [Fact]
    public void Format_WritesTimestampLevelSourceAndMessage()
    {
        var withSource = new LogLine(Stamp, LogLevel.Information, "Connected.", "test");
        var without = new LogLine(Stamp, LogLevel.Warning, "Disk full.");
        var text = LogLine.Format([withSource, without]);
        Assert.Equal(
            "2026-10-06T22:30:00.0000000+00:00 Information test Connected." + Environment.NewLine + "2026-10-06T22:30:00.0000000+00:00 Warning Disk full." + Environment.NewLine,
            text);
    }

    [Fact]
    public void LevelChip_MapsSeverityToHue()
    {
        Assert.Equal((-1, "Trace"), Chip(LogLevel.Trace));
        Assert.Equal((ChipHues.Cyan, "Debug"), Chip(LogLevel.Debug));
        Assert.Equal((ChipHues.Blue, "Info"), Chip(LogLevel.Information));
        Assert.Equal((ChipHues.Amber, "Warn"), Chip(LogLevel.Warning));
        Assert.Equal((ChipHues.Coral, "Error"), Chip(LogLevel.Error));
        Assert.Equal((ChipHues.Rose, "Crit"), Chip(LogLevel.Critical));
    }

    private static (int Hue, string Label) Chip(LogLevel level)
    {
        var chip = LogLine.LevelChip(level);
        return (chip.Hue, chip.Label);
    }
}