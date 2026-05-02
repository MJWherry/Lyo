namespace Lyo.Streams.Tests;

public sealed class CountingStreamTests
{
    [Fact]
    public void BytesRead_tracks_reads()
    {
        var data = "hello!"u8.ToArray();
        var ms = new MemoryStream(data);
        using var counting = new CountingStream(ms);
        var buf = new byte[3];
        counting.ReadExactly(buf, 0, 3);
        Assert.Equal(3L, counting.BytesRead);
        counting.ReadExactly(buf, 0, 3);
        Assert.Equal(6L, counting.BytesRead);
    }

    [Fact]
    public void BytesWritten_tracks_writes()
    {
        var ms = new MemoryStream();
        using var counting = new CountingStream(ms);
        counting.Write("ab"u8.ToArray(), 0, 2);
        Assert.Equal(2L, counting.BytesWritten);
        counting.Write("c"u8.ToArray(), 0, 1);
        Assert.Equal(3L, counting.BytesWritten);
    }

    [Fact]
    public void ResetCounters_zeros_counters()
    {
        var ms = new MemoryStream("x"u8.ToArray());
        using var counting = new CountingStream(ms);
        counting.ReadExactly(new byte[1], 0, 1);
        Assert.Equal(1L, counting.BytesRead);
        counting.ResetCounters();
        Assert.Equal(0L, counting.BytesRead);
        Assert.Equal(0L, counting.BytesWritten);
    }

    [Fact]
    public void Throws_on_null_base_stream() => Assert.Throws<ArgumentNullException>(() => new CountingStream(null!));
}