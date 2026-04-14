using System.Text;

namespace Lyo.Streams.Tests;

public sealed class TeeStreamTests
{
    [Fact]
    public void Writes_to_primary_and_secondary_streams()
    {
        var primary = new MemoryStream();
        var secondary = new MemoryStream();
        var data = Encoding.UTF8.GetBytes("tee data");
        using (var tee = new TeeStream(primary, secondary)) {
            tee.Write(data, 0, data.Length);
            tee.Flush();
        }

        Assert.Equal("tee data", Encoding.UTF8.GetString(primary.ToArray()));
        Assert.Equal("tee data", Encoding.UTF8.GetString(secondary.ToArray()));
    }

    [Fact]
    public void Read_throws_NotSupportedException()
    {
        var primary = new MemoryStream();
        var secondary = new MemoryStream();
        using var tee = new TeeStream(primary, secondary);
        Assert.Throws<NotSupportedException>(() => tee.Read(new byte[1], 0, 1));
    }

    [Fact]
    public void Seek_throws_NotSupportedException()
    {
        var primary = new MemoryStream();
        var secondary = new MemoryStream();
        using var tee = new TeeStream(primary, secondary);
        Assert.Throws<NotSupportedException>(() => tee.Seek(0, SeekOrigin.Begin));
    }

    [Fact]
    public void Throws_on_null_arguments()
    {
        var ms = new MemoryStream();
        Assert.Throws<ArgumentNullException>(() => new TeeStream(null!, ms));
        Assert.Throws<ArgumentNullException>(() => new TeeStream(ms, null!));
        Assert.Throws<ArgumentException>(() => new TeeStream(ms));
    }
}