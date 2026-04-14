using System.Security.Cryptography;

namespace Lyo.Streams.Tests;

public sealed class HashingStreamTests
{
    [Fact]
    public void GetHash_computes_SHA256_of_written_data()
    {
        var data = "hello world"u8.ToArray();
        var expected = SHA256.HashData(data);
        var ms = new MemoryStream();
        using var hashing = new HashingStream(ms, SHA256.Create());
        hashing.Write(data, 0, data.Length);
        var hash = hashing.GetHash();
        Assert.Equal(expected, hash);
    }

    [Fact]
    public void GetHash_computes_SHA256_of_read_data()
    {
        var data = "test"u8.ToArray();
        var expected = SHA256.HashData(data);
        var ms = new MemoryStream(data);
        using var hashing = new HashingStream(ms, SHA256.Create());
        var buffer = new byte[10];
        while (hashing.Read(buffer, 0, buffer.Length) > 0) { }

        var hash = hashing.GetHash();
        Assert.Equal(expected, hash);
    }

    [Fact]
    public void GetHashString_returns_hex_string()
    {
        var data = "x"u8.ToArray();
        var ms = new MemoryStream();
        using var hashing = new HashingStream(ms, SHA256.Create());
        hashing.Write(data, 0, data.Length);
        var hex = hashing.GetHashString();
        Assert.Equal(64, hex.Length);
        Assert.All(hex, c => Assert.True(char.IsAsciiHexDigit(c)));
    }

    [Fact]
    public void GetHash_can_be_called_multiple_times()
    {
        var data = "data"u8.ToArray();
        var ms = new MemoryStream(data);
        using var hashing = new HashingStream(ms, SHA256.Create());
        hashing.ReadExactly(new byte[data.Length], 0, data.Length);
        var h1 = hashing.GetHash();
        var h2 = hashing.GetHash();
        Assert.Equal(h1, h2);
    }

    [Fact]
    public void Throws_on_null_arguments()
    {
        Assert.Throws<ArgumentNullException>(() => new HashingStream(null!, SHA256.Create()));
        Assert.Throws<ArgumentNullException>(() => new HashingStream(new MemoryStream(), null!));
    }
}