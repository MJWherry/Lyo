using Lyo.Privacy.Policy;
using Lyo.Privacy.Rules;

namespace Lyo.Privacy.Tests;

/// <summary>Locks stable audit fingerprint strings across refactors (Hasher / HexEncoding must preserve bytes + casing).</summary>
public sealed class RedactionPolicyFingerprintGoldenTests
{
    [Fact]
    public void Minimal_literal_policy_default_prefix_is_stable_golden()
    {
        var policy = new RedactionPolicyBuilder()
            .WithPolicyName("golden-audit-policy")
            .WithPlaceholder("[MASK]")
            .AddRule(new LiteralSubstringRedactionRule("top-secret-token"))
            .Build();

        var fp = RedactionPolicyFingerprint.ComputeSha256HexPrefix(policy);
        Assert.Equal("c7910c10ff6189e9", fp);
    }

    [Fact]
    public void Hex_char_count_zero_returns_empty_even_when_digest_exists()
    {
        var policy = new RedactionPolicyBuilder().AddRule(new EmailRedactionRule()).Build();
        Assert.Equal(string.Empty, RedactionPolicyFingerprint.ComputeSha256HexPrefix(policy, hexCharCount: 0));
    }

    [Fact]
    public void Prefix_truncation_matches_first_chars_of_full_digest_hex()
    {
        var policy = new RedactionPolicyBuilder().AddRule(new EmailRedactionRule()).Build();
        var full = RedactionPolicyFingerprint.ComputeSha256HexPrefix(policy, hexCharCount: 64);
        Assert.Equal(64, full.Length);
        Assert.Equal(full[..16], RedactionPolicyFingerprint.ComputeSha256HexPrefix(policy, hexCharCount: 16));
        Assert.Equal(full[..8], RedactionPolicyFingerprint.ComputeSha256HexPrefix(policy, hexCharCount: 8));
    }
}
