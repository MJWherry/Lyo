using System.Security.Cryptography;
using Lyo.Common.Core.Enums;

namespace Lyo.Hashing.Tests;

public sealed class HashingStreamTests
{
    [Fact]
    public void GetHash_Computes_SHA256_Of_Written_Data()
    {
        var data = "hello world"u8.ToArray();
        var expected = Hasher.ComputeSha256(data);
        var ms = new MemoryStream();
        using var hashing = new HashingStream(ms, SHA256.Create());
        hashing.Write(data, 0, data.Length);
        var hash = hashing.GetHash();
        Assert.Equal(expected, hash);
    }

    [Fact]
    public void GetHash_Computes_SHA256_Of_Read_Data()
    {
        var data = "test"u8.ToArray();
        var expected = Hasher.ComputeSha256(data);
        var ms = new MemoryStream(data);
        using var hashing = new HashingStream(ms, SHA256.Create());
        var buffer = new byte[10];
        while (hashing.Read(buffer, 0, buffer.Length) > 0) { }

        var hash = hashing.GetHash();
        Assert.Equal(expected, hash);
    }

    [Fact]
    public void GetHashHex_Upper_Matches_GetHashString()
    {
        var data = "x"u8.ToArray();
        var ms = new MemoryStream();
        using var hashing = new HashingStream(ms, SHA256.Create());
        hashing.Write(data, 0, data.Length);
        Assert.Equal(hashing.GetHashString(), hashing.GetHashHex(TextLetterCase.Upper));
    }

    [Fact]
    public void GetHash_Can_Be_Called_Multiple_Times()
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
    public void Throws_On_Null_Arguments()
    {
        Assert.Throws<ArgumentNullException>(() => new HashingStream(null!, SHA256.Create()));
        Assert.Throws<ArgumentNullException>(() => new HashingStream(new MemoryStream(), null!));
    }
}