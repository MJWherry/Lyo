using System.Text;

namespace Lyo.Streams.Tests;

public sealed class ProgressStreamTests
{
    [Fact]
    public void Reports_write_progress()
    {
        var dest = new MemoryStream();
        var reported = new List<long>();
        var progress = new Progress<long>(v => reported.Add(v));
        var data = Encoding.UTF8.GetBytes("hello");
        using var progressStream = new ProgressStream(dest, writeProgress: progress);
        progressStream.Write(data, 0, data.Length);
        Assert.Equal(5L, progressStream.TotalBytesWritten);
        Assert.Equal(5, dest.Length);
    }

    [Fact]
    public void Reports_read_progress()
    {
        var src = new MemoryStream(Encoding.UTF8.GetBytes("abc"));
        var progress = new Progress<long>(_ => { });
        var buffer = new byte[3];
        using var progressStream = new ProgressStream(src, progress);
        progressStream.ReadExactly(buffer, 0, buffer.Length);
        Assert.Equal(3L, progressStream.TotalBytesRead);
    }

    [Fact]
    public void TotalBytesWritten_tracks_writes()
    {
        var dest = new MemoryStream();
        using var progressStream = new ProgressStream(dest);
        progressStream.Write(Encoding.UTF8.GetBytes("12"), 0, 2);
        Assert.Equal(2L, progressStream.TotalBytesWritten);
        progressStream.Write(Encoding.UTF8.GetBytes("345"), 0, 3);
        Assert.Equal(5L, progressStream.TotalBytesWritten);
    }

    [Fact]
    public void Throws_on_null_base_stream() => Assert.Throws<ArgumentNullException>(() => new ProgressStream(null!));
}