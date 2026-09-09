namespace Lyo.Streams.Tests;

public sealed class NullingStreamTests
{
    [Fact]
    public void Write_Discards_And_Counts_Bytes()
    {
        using var stream = new NullingStream();
        stream.Write(new byte[100], 0, 100);
        stream.Write(new byte[50], 0, 50);
        Assert.Equal(150L, stream.BytesWritten);
    }

    [Fact]
    public void ResetCounter_Zeros_Count()
    {
        using var stream = new NullingStream();
        stream.Write(new byte[10], 0, 10);
        stream.ResetCounter();
        Assert.Equal(0L, stream.BytesWritten);
    }

    [Fact]
    public void Read_And_Seek_Throw()
    {
        using var stream = new NullingStream();
        Assert.Throws<NotSupportedException>(() => stream.Read(new byte[1], 0, 1));
        Assert.Throws<NotSupportedException>(() => stream.Seek(0, SeekOrigin.Begin));
    }

    [Fact]
    public async Task WriteAsync_Counts_Bytes()
    {
        await using var stream = new NullingStream();
        await stream.WriteAsync(new byte[32].AsMemory(), TestContext.Current.CancellationToken);
        Assert.Equal(32L, stream.BytesWritten);
    }
}