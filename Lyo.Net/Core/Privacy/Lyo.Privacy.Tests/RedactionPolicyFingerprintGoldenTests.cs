using Lyo.Privacy.Policy;
using Lyo.Privacy.Rules;

namespace Lyo.Privacy.Tests;

/// <summary>Pins stable audit fingerprint strings across refactors (Hasher / HexEncoding must preserve bytes and casing).</summary>
public sealed class RedactionPolicyFingerprintGoldenTests
{
    [Fact]
    public void Minimal_Literal_Policy_Default_Prefix_Is_Stable_Golden()
    {
        var policy = new RedactionPolicyBuilder().WithPolicyName("golden-audit-policy")
            .WithPlaceholder("[MASK]")
            .AddRule(new LiteralSubstringRedactionRule("top-secret-token"))
            .Build();

        var fp = RedactionPolicyFingerprint.ComputeSha256HexPrefix(policy);
        Assert.Equal("c7910c10ff6189e9", fp);
    }

    [Fact]
    public void Hex_Char_Count_Zero_Returns_Empty_Even_When_Digest_Exists()
    {
        var policy = new RedactionPolicyBuilder().AddRule(new EmailRedactionRule()).Build();
        Assert.Equal(string.Empty, RedactionPolicyFingerprint.ComputeSha256HexPrefix(policy, 0));
    }

    [Fact]
    public void Prefix_Truncation_Matches_First_Chars_Of_Full_Digest_Hex()
    {
        var policy = new RedactionPolicyBuilder().AddRule(new EmailRedactionRule()).Build();
        var full = RedactionPolicyFingerprint.ComputeSha256HexPrefix(policy, 64);
        Assert.Equal(64, full.Length);
        Assert.Equal(full[..16], RedactionPolicyFingerprint.ComputeSha256HexPrefix(policy));
        Assert.Equal(full[..8], RedactionPolicyFingerprint.ComputeSha256HexPrefix(policy, 8));
    }
}