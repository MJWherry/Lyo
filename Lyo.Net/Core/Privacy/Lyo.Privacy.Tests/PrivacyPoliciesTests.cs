using Lyo.Privacy.Configuration;
using Lyo.Privacy.Enums;
using Lyo.Privacy.Policy;
using Lyo.Privacy.Rules;
using Lyo.Privacy.Text;

namespace Lyo.Privacy.Tests;

public sealed class PrivacyPoliciesTests
{
    [Fact]
    public void SupportExport_redacts_bearer_and_jwt_shape()
    {
        var r = new TextRedactor(PrivacyPolicies.SupportExport());
        var jwt = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0In0.sig";
        var res = r.Redact("Authorization: Bearer abc.def.ghi " + jwt);
        Assert.DoesNotContain("Bearer abc.def.ghi", res.Text);
        Assert.DoesNotContain(jwt, res.Text);
        Assert.Contains("[redacted]", res.Text);
    }

    [Fact]
    public void RegressionTesting_uses_bracket_placeholder()
    {
        var r = new TextRedactor(PrivacyPolicies.RegressionTesting());
        var res = r.Redact("a@b.co");
        Assert.Equal("[REDACTED]", res.Text);
    }

    [Fact]
    public void AppendPreset_unknown_throws()
    {
        var b = new RedactionPolicyBuilder();
        Assert.Throws<ArgumentException>(() => b.AppendPreset("nope"));
    }

    [Fact]
    public void Composite_rule_covers_all_inner_spans()
    {
        var inner = new CompositeRedactionRule([new LiteralSubstringRedactionRule("AAA")]);
        var r = new TextRedactor(new RedactionPolicyBuilder().AddRule(inner).Build());
        var res = r.Redact("x AAA y");
        Assert.Equal("x [redacted] y", res.Text);
        Assert.Equal(1, res.CountsByKind[RedactionKind.Composite]);
    }
}