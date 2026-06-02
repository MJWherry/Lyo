using Lyo.Authentication.Format;
using Lyo.Authentication.Models.Format;

namespace Lyo.Authentication.Tests;

public class ApiTokenCodecTests
{
    [Fact]
    public void Mint_ProducesParsableTokenWithCorrectShape()
    {
        var (plaintext, id, hash) = ApiTokenCodec.Mint(ApiTokenKind.Pat, ApiTokenRing.Live);
        Assert.StartsWith("lyo_pat_live_", plaintext);
        Assert.Equal(11, id.Length);
        Assert.Equal(32, hash.Length);
        Assert.True(ApiTokenCodec.TryParse(plaintext, out var parsed));
        Assert.NotNull(parsed);
        Assert.Equal(id, parsed.Id);
        Assert.Equal(ApiTokenKind.Pat, parsed.Kind);
        Assert.Equal(ApiTokenRing.Live, parsed.Ring);
        Assert.Equal(43, parsed.Secret.Length);
    }

    [Fact]
    public void Mint_TwoCalls_ProduceDistinctIdsAndSecrets()
    {
        var (a, idA, hashA) = ApiTokenCodec.Mint(ApiTokenKind.Cli, ApiTokenRing.Dev);
        var (b, idB, hashB) = ApiTokenCodec.Mint(ApiTokenKind.Cli, ApiTokenRing.Dev);
        Assert.NotEqual(a, b);
        Assert.NotEqual(idA, idB);
        Assert.NotEqual(Convert.ToBase64String(hashA), Convert.ToBase64String(hashB));
    }

    [Fact]
    public void Mint_RecomputingHashOverSecret_MatchesStoredHash()
    {
        var (plaintext, _, hash) = ApiTokenCodec.Mint(ApiTokenKind.Svc, ApiTokenRing.Test);
        Assert.True(ApiTokenCodec.TryParse(plaintext, out var parsed));
        Assert.NotNull(parsed);
        var recomputed = ApiTokenCodec.ComputeSecretHash(parsed.Secret);
        Assert.Equal(hash, recomputed);
    }

    [Theory]
    [InlineData("")]
    [InlineData("notatoken")]
    [InlineData("lyo_pat_live")]
    [InlineData("lyo_pat_live_abc_xyz")]
    [InlineData("lyo_PAT_live_01hxy8k2qf9_4f3b7a2c9e8d1b6a5c4e7f8a9b0c1d2e3f4a5b6c7d8")]
    [InlineData("foo_pat_live_01hxy8k2qf9_4f3b7a2c9e8d1b6a5c4e7f8a9b0c1d2e3f4a5b6c7d8")]
    public void TryParse_RejectsMalformedInputs(string? input)
    {
        Assert.False(ApiTokenCodec.TryParse(input, out var parsed));
        Assert.Null(parsed);
    }

    [Fact]
    public void TryParse_RejectsBadSecretLength()
    {
        var (plaintext, _, _) = ApiTokenCodec.Mint(ApiTokenKind.Pat, ApiTokenRing.Live);
        var truncated = plaintext.Substring(0, plaintext.Length - 1);
        Assert.False(ApiTokenCodec.TryParse(truncated, out var _));
    }

    [Fact]
    public void TryParse_RejectsBadId()
    {
        var bad = "lyo_pat_live_uuuuuuuuuuu_" + new string('A', 43);
        Assert.False(ApiTokenCodec.TryParse(bad, out var _));
    }

    [Fact]
    public void TryParse_AcceptsSecretsContainingUnderscores()
    {
        for (var i = 0; i < 500; i++) {
            var (plaintext, _, _) = ApiTokenCodec.Mint(ApiTokenKind.Pat, ApiTokenRing.Live);
            Assert.True(ApiTokenCodec.TryParse(plaintext, out var _), $"Failed on '{plaintext}'");
        }
    }

    [Fact]
    public void Mint_WithUppercaseKind_Throws() => Assert.Throws<ArgumentException>(() => ApiTokenCodec.Mint("PAT", ApiTokenRing.Live));

    [Fact]
    public void Mint_AcrossManyIterations_ProducesUniqueIds()
    {
        var ids = new HashSet<string>();
        for (var i = 0; i < 5000; i++) {
            var (_, id, _) = ApiTokenCodec.Mint(ApiTokenKind.Pat, ApiTokenRing.Live);
            Assert.True(ids.Add(id), $"Duplicate id on iteration {i}: {id}");
        }
    }
}