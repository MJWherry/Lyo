using Lyo.Privacy.Configuration;
using Lyo.Privacy.Enums;
using Lyo.Privacy.Policy;
using Lyo.Privacy.Rules;
using Lyo.Privacy.Text;

namespace Lyo.Privacy.Tests;

public sealed class PrivacyPoliciesTests
{
    [Fact]
    public void SupportExport_Redacts_Bearer_And_Jwt_Shape()
    {
        var r = new TextRedactor(PrivacyPolicies.SupportExport());
        var jwt = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0In0.sig";
        var res = r.Redact("Authorization: Bearer abc.def.ghi " + jwt);
        Assert.DoesNotContain("Bearer abc.def.ghi", res.Text);
        Assert.DoesNotContain(jwt, res.Text);
        Assert.Contains("[redacted]", res.Text);
    }

    [Fact]
    public void RegressionTesting_Redacts_Us_Ssn_Shape()
    {
        var r = new TextRedactor(PrivacyPolicies.RegressionTesting());
        var res = r.Redact("123-45-6789");
        Assert.Equal("[REDACTED]", res.Text);
    }

    [Fact]
    public void Logging_Redacts_Us_Ssn_Shape()
    {
        var r = new TextRedactor(PrivacyPolicies.Logging());
        var res = r.Redact("123-45-6789");
        Assert.Equal("[redacted]", res.Text);
        Assert.Equal(1, res.CountsByKind[RedactionKind.TaxId]);
    }

    [Fact]
    public void PolicyJson_Round_Trips_Regression_Preset()
    {
        var original = PrivacyPolicies.RegressionTesting();
        var json = PolicyJson.SerializePolicy(original);
        var rebuilt = PolicyJson.Build(json);
        var r = new TextRedactor(rebuilt);
        Assert.Equal("[REDACTED]", r.Redact("123-45-6789").Text);
        Assert.Equal("[REDACTED]", r.Redact("a@b.co").Text);
    }

    [Fact]
    public void AppendPreset_Unknown_Throws()
    {
        var b = new RedactionPolicyBuilder();
        Assert.Throws<ArgumentException>(() => b.AppendPreset("nope"));
    }

    [Fact]
    public void Composite_Rule_Covers_All_Inner_Spans()
    {
        var inner = new CompositeRedactionRule([new LiteralSubstringRedactionRule("AAA")]);
        var r = new TextRedactor(new RedactionPolicyBuilder().AddRule(inner).Build());
        var res = r.Redact("x AAA y");
        Assert.Equal("x [redacted] y", res.Text);
        Assert.Equal(1, res.CountsByKind[RedactionKind.Composite]);
    }
}