using Lyo.Privacy.Policy;
using Lyo.Privacy.Rules;
using Lyo.Privacy.Text;

namespace Lyo.Privacy.Tests;

public sealed class RedactionResultTests
{
    [Fact]
    public void ToString_does_not_echo_redacted_text()
    {
        var policy = new RedactionPolicyBuilder().WithPolicyName("logging").AddRule(new EmailRedactionRule()).Build();
        var r = new TextRedactor(policy);
        var res = r.Redact("secret alice@evilcorp.com end");
        Assert.DoesNotContain("alice", res.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("evilcorp", res.ToString(), StringComparison.Ordinal);
        Assert.Contains("Email", res.ToString(), StringComparison.Ordinal);
        Assert.Contains("logging", res.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Result_carries_length_and_policy_metadata()
    {
        var policy = new RedactionPolicyBuilder().WithPolicyName("t").AddRule(new EmailRedactionRule()).Build();
        var res = new TextRedactor(policy).Redact("aa@bb.co");
        Assert.Equal(8, res.InputUtf16Length);
        Assert.NotNull(res.OutputUtf16Length);
        Assert.Equal("t", res.PolicyName);
        Assert.True(res.HadRedactions);
    }
}