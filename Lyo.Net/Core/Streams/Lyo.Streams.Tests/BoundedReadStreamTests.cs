namespace Lyo.Streams.Tests;

public sealed class BoundedReadStreamTests
{
    [Fact]
    public void Read_StopsAtCap_ThenReturnsZero()
    {
        var inner = new MemoryStream([1, 2, 3, 4, 5]);
        using var bounded = new BoundedReadStream(inner, 3);
        var buffer = new byte[8];

        Assert.Equal(3, bounded.Read(buffer, 0, buffer.Length));
        Assert.Equal(new byte[] { 1, 2, 3 }, buffer[..3]);
        Assert.Equal(0, bounded.Read(buffer, 0, buffer.Length));
    }

    [Fact]
    public async Task ReadAsync_StopsAtCap_ThenReturnsZero()
    {
        var inner = new MemoryStream([1, 2, 3, 4, 5]);
        await using var bounded = new BoundedReadStream(inner, 3);
        var buffer = new byte[2];

        Assert.Equal(2, await bounded.ReadAsync(buffer, 0, buffer.Length, TestCt));
        Assert.Equal(new byte[] { 1, 2 }, buffer);
        Assert.Equal(1, await bounded.ReadAsync(buffer, 0, 1, TestCt));
        Assert.Equal(3, buffer[0]);
        Assert.Equal(0, await bounded.ReadAsync(buffer, 0, buffer.Length, TestCt));
    }

    [Fact]
    public void Dispose_DisposesInner()
    {
        var inner = new MemoryStream([1, 2, 3]);
        var bounded = new BoundedReadStream(inner, 2);

        bounded.Dispose();

        Assert.Throws<ObjectDisposedException>(() => inner.ReadByte());
    }

    [Fact]
    public void Throws_On_Null_Inner() => Assert.Throws<ArgumentNullException>(() => new BoundedReadStream(null!, 1));
}
