using Lyo.Common.Enums;

namespace Lyo.Hashing.Tests;

public sealed class HashingServiceTests
{
    [Fact]
    public void Shared_singleton_matches_hasher_static()
    {
        var svc = HashingService.Shared;
        var payload = "a"u8.ToArray();
        var fromSvc = svc.Hash(ContentDigestAlgorithm.Sha256, payload);
        Assert.Equal(Hasher.ComputeSha256(payload), fromSvc);
        Assert.Equal(HexEncoding.ToHexString(fromSvc), svc.ToHex(fromSvc));
    }

    [Fact]
    public void FixedTimeEquals_and_EqualsHex()
    {
        var svc = HashingService.Shared;
        var digest = new byte[] { 1, 2, 3 };
        Assert.True(svc.FixedTimeEquals(digest, digest));
        var hex = HexEncoding.ToHexString(digest, TextLetterCase.Lower);
        Assert.True(svc.EqualsHex(digest, hex));
        Assert.False(svc.EqualsHex(digest, "ff"));
        Assert.False(svc.EqualsHex(digest, "zzz"));
        Assert.Equal(digest, svc.ParseHex(hex));
    }

    [Fact]
    public void HmacSmoke()
    {
        var k = "key"u8.ToArray();
        var p = "payload"u8.ToArray();
        var svc = HashingService.Shared;
        Assert.Equal(32, svc.HmacSha256(k, p).Length);
        Assert.Equal(64, svc.HmacSha512(k, p).Length);
    }

    [Fact]
    public void CreateHashingStream_wraps_underlying_stream()
    {
        using var inner = new MemoryStream();
        using var hs = HashingService.Shared.CreateHashingStream(inner, ContentDigestAlgorithm.Sha256);
        hs.Write("z"u8.ToArray(), 0, 1);
        var digest = hs.GetHash();
        Assert.NotEmpty(digest);
    }
}