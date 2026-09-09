namespace Lyo.Streams.Tests;

public sealed class StreamChunkSizeHelperTests
{
    [Fact]
    public void DetermineChunkSize_Null_Stream_Returns_Default()
    {
        var size = StreamChunkSizeHelper.DetermineChunkSize((Stream?)null);
        Assert.True(size >= 64 * 1024);
    }

    [Fact]
    public void DetermineChunkSize_Small_Stream_Returns_Min_Chunk()
    {
        var ms = new MemoryStream(new byte[100]);
        var size = StreamChunkSizeHelper.DetermineChunkSize(ms);
        Assert.Equal(64 * 1024, size);
    }

    [Fact]
    public void DetermineChunkSize_Medium_Stream_Returns_Default_Chunk()
    {
        var size = StreamChunkSizeHelper.DetermineChunkSize(50L * 1024 * 1024);
        Assert.Equal(1024 * 1024, size);
    }

    [Fact]
    public void DetermineChunkSize_Large_Stream_Returns_Larger_Chunk()
    {
        var opts = new StreamChunkSizeOptions();
        var size = StreamChunkSizeHelper.DetermineChunkSize(2L * 1024 * 1024 * 1024, opts);
        Assert.Equal(5 * 1024 * 1024, size);
    }

    [Fact]
    public void DetermineChunkSize_Zero_Returns_Default()
    {
        var size = StreamChunkSizeHelper.DetermineChunkSize(0L);
        Assert.Equal(1024 * 1024, size);
    }

    [Fact]
    public void DetermineChunkSize_Custom_Options_Uses_Thresholds()
    {
        var opts = new StreamChunkSizeOptions { MinChunkSize = 4096, SmallFileThreshold = 1000 };
        var size = StreamChunkSizeHelper.DetermineChunkSize(500, opts);
        Assert.Equal(4096, size);
    }
}