namespace Lyo.Streams.Tests;

public sealed class CountingStreamTests
{
    [Fact]
    public void BytesRead_Tracks_Reads()
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
    public void BytesWritten_Tracks_Writes()
    {
        var ms = new MemoryStream();
        using var counting = new CountingStream(ms);
        counting.Write("ab"u8.ToArray(), 0, 2);
        Assert.Equal(2L, counting.BytesWritten);
        counting.Write("c"u8.ToArray(), 0, 1);
        Assert.Equal(3L, counting.BytesWritten);
    }

    [Fact]
    public void ResetCounters_Zeros_Counters()
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
    public void Throws_On_Null_Base_Stream() => Assert.Throws<ArgumentNullException>(() => new CountingStream(null!));
}