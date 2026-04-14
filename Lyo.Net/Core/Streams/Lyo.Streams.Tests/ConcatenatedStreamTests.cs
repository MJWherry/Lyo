using System.Text;

namespace Lyo.Streams.Tests;

public sealed class ConcatenatedStreamTests
{
    [Fact]
    public void Reads_concatenated_content_from_multiple_streams()
    {
        var s1 = new MemoryStream(Encoding.UTF8.GetBytes("Hello "));
        var s2 = new MemoryStream(Encoding.UTF8.GetBytes("World"));
        using var concat = new ConcatenatedStream([s1, s2]);
        using var reader = new StreamReader(concat);
        var result = reader.ReadToEnd();
        Assert.Equal("Hello World", result);
    }

    [Fact]
    public void ReadAsync_returns_concatenated_content()
    {
        var s1 = new MemoryStream(Encoding.UTF8.GetBytes("A"));
        var s2 = new MemoryStream(Encoding.UTF8.GetBytes("B"));
        var s3 = new MemoryStream(Encoding.UTF8.GetBytes("C"));
        using var concat = new ConcatenatedStream([s1, s2, s3]);
        var buffer = new byte[10];
        var total = 0;
        int read;
        while ((read = concat.Read(buffer, total, buffer.Length - total)) > 0)
            total += read;

        Assert.Equal("ABC", Encoding.UTF8.GetString(buffer, 0, total));
    }

    [Fact]
    public void Throws_on_null_streams()
    {
        Assert.Throws<ArgumentNullException>(() => new ConcatenatedStream(null!));
        Assert.Throws<ArgumentException>(() => new ConcatenatedStream([new MemoryStream(), null!]));
    }

    [Fact]
    public void Throws_on_empty_streams() => Assert.Throws<InvalidOperationException>(() => new ConcatenatedStream([]));

    [Fact]
    public void Length_throws_NotSupportedException()
    {
        using var concat = new ConcatenatedStream([new MemoryStream(Encoding.UTF8.GetBytes("x"))]);
        Assert.Throws<NotSupportedException>(() => _ = concat.Length);
    }

    [Fact]
    public void Seek_throws_NotSupportedException()
    {
        using var concat = new ConcatenatedStream([new MemoryStream(Encoding.UTF8.GetBytes("x"))]);
        Assert.Throws<NotSupportedException>(() => concat.Seek(0, SeekOrigin.Begin));
    }

    [Fact]
    public void Write_throws_NotSupportedException()
    {
        using var concat = new ConcatenatedStream([new MemoryStream(Encoding.UTF8.GetBytes("x"))]);
        var buf = new byte[1];
        Assert.Throws<NotSupportedException>(() => concat.Write(buf, 0, 1));
    }

    [Fact]
    public void Params_constructor_works()
    {
        using var concat = new ConcatenatedStream(false, new MemoryStream(Encoding.UTF8.GetBytes("a")), new MemoryStream(Encoding.UTF8.GetBytes("b")));
        using var reader = new StreamReader(concat);
        Assert.Equal("ab", reader.ReadToEnd());
    }
}