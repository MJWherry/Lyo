using Lyo.Media.Models;

namespace Lyo.FFmpeg.Tests;

public class FFmpegProgressParserTests
{
    [Fact]
    public void Ingest_OutTimeUs_ReportsMicroseconds()
    {
        var snapshots = new List<MediaProgress>();
        var parser = new FFmpegProgressParser(new SyncProgress<MediaProgress>(snapshots.Add), TimeSpan.FromSeconds(2));
        parser.IngestLine("out_time_us=1500000");
        parser.IngestLine("progress=continue");
        Assert.Single(snapshots);
        Assert.Equal(TimeSpan.FromSeconds(1.5), snapshots[0].OutTime);
        Assert.Equal(75, snapshots[0].Percentage);
    }

    [Fact]
    public void Ingest_OutTimeMs_TreatedAsMicroseconds()
    {
        var snapshots = new List<MediaProgress>();
        var parser = new FFmpegProgressParser(new SyncProgress<MediaProgress>(snapshots.Add), knownDuration: null);
        parser.IngestLine("out_time_ms=500000");
        parser.IngestLine("progress=continue");
        Assert.Equal(TimeSpan.FromMilliseconds(500), snapshots[0].OutTime);
    }

    [Fact]
    public void Ingest_OutTime_ParsesTimestampWhenUsMissing()
    {
        var snapshots = new List<MediaProgress>();
        var parser = new FFmpegProgressParser(new SyncProgress<MediaProgress>(snapshots.Add), knownDuration: null);
        parser.IngestLine("out_time=00:00:01.250000");
        parser.IngestLine("progress=continue");
        Assert.Equal(TimeSpan.FromSeconds(1.25), snapshots[0].OutTime);
    }

    [Fact]
    public void Ingest_OutTimeUs_WinsOverOutTime()
    {
        var snapshots = new List<MediaProgress>();
        var parser = new FFmpegProgressParser(new SyncProgress<MediaProgress>(snapshots.Add), knownDuration: null);
        parser.IngestLine("out_time_us=1000000");
        parser.IngestLine("out_time=00:00:09.000000");
        parser.IngestLine("progress=continue");
        Assert.Equal(TimeSpan.FromSeconds(1), snapshots[0].OutTime);
    }

    [Fact]
    public void Ingest_SpeedAndFrame()
    {
        var snapshots = new List<MediaProgress>();
        var parser = new FFmpegProgressParser(new SyncProgress<MediaProgress>(snapshots.Add), TimeSpan.FromSeconds(10));
        parser.IngestLine("frame=42");
        parser.IngestLine("speed=1.50x");
        parser.IngestLine("out_time_us=2000000");
        parser.IngestLine("progress=continue");
        Assert.Equal(42, snapshots[0].Frame);
        Assert.Equal(1.5, snapshots[0].Speed);
        Assert.Equal(20, snapshots[0].Percentage);
    }

    private sealed class SyncProgress<T>(Action<T> callback) : IProgress<T>
    {
        public void Report(T value) => callback(value);
    }
}
