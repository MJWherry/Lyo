namespace Lyo.Streams.Tests;

public sealed class StreamChunkSizeHelperTests
{
    [Fact]
    public void DetermineChunkSize_null_stream_returns_default()
    {
        var size = StreamChunkSizeHelper.DetermineChunkSize((Stream?)null);
        Assert.True(size >= 64 * 1024);
    }

    [Fact]
    public void DetermineChunkSize_small_stream_returns_min_chunk()
    {
        var ms = new MemoryStream(new byte[100]);
        var size = StreamChunkSizeHelper.DetermineChunkSize(ms);
        Assert.Equal(64 * 1024, size);
    }

    [Fact]
    public void DetermineChunkSize_medium_stream_returns_default_chunk()
    {
        var size = StreamChunkSizeHelper.DetermineChunkSize(50L * 1024 * 1024);
        Assert.Equal(1024 * 1024, size);
    }

    [Fact]
    public void DetermineChunkSize_large_stream_returns_larger_chunk()
    {
        var opts = new StreamChunkSizeOptions();
        var size = StreamChunkSizeHelper.DetermineChunkSize(2L * 1024 * 1024 * 1024, opts);
        Assert.Equal(5 * 1024 * 1024, size);
    }

    [Fact]
    public void DetermineChunkSize_zero_returns_default()
    {
        var size = StreamChunkSizeHelper.DetermineChunkSize(0L);
        Assert.Equal(1024 * 1024, size);
    }

    [Fact]
    public void DetermineChunkSize_custom_options_uses_thresholds()
    {
        var opts = new StreamChunkSizeOptions { MinChunkSize = 4096, SmallFileThreshold = 1000 };
        var size = StreamChunkSizeHelper.DetermineChunkSize(500, opts);
        Assert.Equal(4096, size);
    }
}