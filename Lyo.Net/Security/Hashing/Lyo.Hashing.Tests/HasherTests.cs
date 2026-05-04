using System.Text;
using Lyo.Common.Enums;

namespace Lyo.Hashing.Tests;

public sealed class HasherTests
{
    [Fact]
    public void ComputeSha512_empty_matches_known_hex_when_encoded()
        => Assert.Equal(
            "cf83e1357eefb8bdf1542850d66d8007d620e4050b5715dc83f4a921d36ce9ce47d0d13c5d85f2b0ff8318d2877eec2f63b931bd47417a81a538327af927da3e",
            HexEncoding.ToHexString(Hasher.ComputeSha512(Array.Empty<byte>()), TextLetterCase.Lower));

    [Fact]
    public void ComputeSha256_abc_known_vector_bytes()
    {
        var digest = Hasher.ComputeSha256("abc"u8.ToArray());
        Assert.Equal(32, digest.Length);
        Assert.Equal("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad", HexEncoding.ToHexString(digest, TextLetterCase.Lower));
    }

    [Fact]
    public void ComputeSha256_stream_matches_bytes()
    {
        var bytes = Encoding.UTF8.GetBytes("stream sha256 test");
        using var ms = new MemoryStream(bytes);
        var fromStream = Hasher.ComputeSha256(ms);
        Assert.Equal(ms.Length, ms.Position);
        Assert.Equal(fromStream, Hasher.ComputeSha256(bytes));
    }

    [Fact]
    public void ComputeMd5_roundtrip_bytes()
    {
        var bytes = "md5"u8.ToArray();
        Assert.Equal(16, Hasher.ComputeMd5(bytes).Length);
        Assert.Equal(Hasher.ComputeMd5(bytes), Hasher.ComputeMd5(bytes.AsSpan()));
    }

    [Fact]
    public void ComputeSha384_convenience_matches_ComputeSha2()
    {
        var data = "384"u8.ToArray();
        Assert.Equal(Hasher.ComputeSha2(384, data), Hasher.ComputeSha384(data));
    }

    [Fact]
    public void ComputeSha512_convenience_matches_ComputeSha2()
    {
        var data = Array.Empty<byte>();
        Assert.Equal(Hasher.ComputeSha2(512, data), Hasher.ComputeSha512(data));
    }

    [Fact]
    public void ComputeSha2_unknown_digest_bits_throws() => Assert.Throws<ArgumentOutOfRangeException>(() => Hasher.ComputeSha2(128, ReadOnlySpan<byte>.Empty));
}