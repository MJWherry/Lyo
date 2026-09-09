using Lyo.Privacy.Configuration;
using Lyo.Privacy.Enums;
using Lyo.Privacy.Policy;
using Lyo.Privacy.Rules;
using Lyo.Privacy.Text;

namespace Lyo.Privacy.Tests;

public sealed class TextRedactorTests
{
    [Fact]
    public void Redact_Email_Replaces_With_Placeholder()
    {
        var r = new TextRedactor(PrivacyPolicies.Minimal(b => b.AddRule(new EmailRedactionRule())));
        var res = r.Redact("Contact alice@example.com today");
        Assert.Equal("Contact [redacted] today", res.Text);
        Assert.Equal(1, res.CountsByKind[RedactionKind.Email]);
    }

    [Fact]
    public void Redact_Url_Strips_Query_Only()
    {
        var r = new TextRedactor(PrivacyPolicies.Minimal(b => b.AddRule(new UrlRedactionRule())));
        var res = r.Redact("See https://api.example/v1?id=1&token=secret");
        Assert.Equal("See https://api.example/v1[redacted]", res.Text);
        Assert.Equal(1, res.CountsByKind[RedactionKind.Url]);
    }

    [Fact]
    public void Redact_Luhn_Card_Keeps_Last_Four()
    {
        var r = new TextRedactor(PrivacyPolicies.Minimal(b => b.AddRule(new PaymentCardRedactionRule())));
        var res = r.Redact("PAN 4111111111111111 ok");
        Assert.Equal("PAN [redacted]1111 ok", res.Text);
        Assert.Equal(1, res.CountsByKind[RedactionKind.PaymentCard]);
    }

    [Fact]
    public void Redact_Non_Luhn_Digit_Run_Not_Touched()
    {
        var r = new TextRedactor(PrivacyPolicies.Minimal(b => b.AddRule(new PaymentCardRedactionRule())));
        var res = r.Redact("id 12345678901234");
        Assert.Equal("id 12345678901234", res.Text);
        Assert.Equal(0, res.TotalRuns);
    }

    [Fact]
    public void Redact_Ipv4_Truncate_Mode_Only_Masks_Last_Octet()
    {
        var r = new TextRedactor(PrivacyPolicies.Minimal(b => b.AddRule(new IpAddressRedactionRule(IpRedactionMode.TruncateLastSegment))));
        var res = r.Redact("host 203.0.113.44");
        Assert.Equal("host 203.0.113.[redacted]", res.Text);
        Assert.Equal(1, res.CountsByKind[RedactionKind.IpAddress]);
    }

    [Fact]
    public void Redact_Literal_Case_Insensitive()
    {
        var r = new TextRedactor(PrivacyPolicies.Minimal(b => b.AddRule(new LiteralSubstringRedactionRule("ACME Corp"))));
        var res = r.Redact("acme corp and Acme Corp");
        Assert.Equal("[redacted] and [redacted]", res.Text);
        Assert.Equal(2, res.CountsByKind[RedactionKind.Literal]);
    }

    [Fact]
    public void Redact_Merge_Adjacent_False_Emits_Per_Char_For_Same_Run()
    {
        var policy = new RedactionPolicyBuilder().WithMergeAdjacentRuns(false).AddRule(new EmailRedactionRule()).Build();
        var r = new TextRedactor(policy);
        var res = r.Redact("x@y.co");
        var expectedLen = "x@y.co".Length;
        var expected = string.Concat(Enumerable.Repeat("[redacted]", expectedLen));
        Assert.Equal(expected, res.Text);
    }

    [Fact]
    public void Earlier_Rule_Wins_Overlap()
    {
        var policy = new RedactionPolicyBuilder().AddRule(new LiteralSubstringRedactionRule("foo@bar.com")).AddRule(new EmailRedactionRule()).Build();
        var r = new TextRedactor(policy);
        var res = r.Redact("hello foo@bar.com end");
        Assert.Equal("hello [redacted] end", res.Text);
        Assert.Equal(1, res.CountsByKind[RedactionKind.Literal]);
    }

    [Fact]
    public void Null_Input_Yields_Null_Text()
    {
        var r = new TextRedactor(PrivacyPolicies.Logging());
        var res = r.Redact(null);
        Assert.Null(res.Text);
        Assert.Empty(res.CountsByKind);
    }

    [Fact]
    public void Phone_Last_Digits_Keeps_Suffix()
    {
        var r = new TextRedactor(PrivacyPolicies.Minimal(b => b.AddRule(new PhoneRedactionRule(PhoneMaskMode.LastDigits, minDigits: 10))));
        var res = r.Redact("call +1-555-123-4567");
        Assert.NotNull(res.Text);
        Assert.Contains("4567", res.Text);
        Assert.Contains("*", res.Text);
        Assert.DoesNotContain("[redacted]", res.Text);
    }

    [Fact]
    public void Phone_Matches_Do_Not_Bridge_Newlines()
    {
        var r = new TextRedactor(PrivacyPolicies.Minimal(b => b.AddRule(new PhoneRedactionRule(PhoneMaskMode.LastDigits, minDigits: 10))));
        var res = r.Redact("123-456-7890\n987-654-3210");
        Assert.NotNull(res.Text);
        var lines = res.Text.Split('\n');
        Assert.Equal(2, lines.Length);
        Assert.Contains("7890", lines[0]);
        Assert.Contains("3210", lines[1]);
    }

    [Fact]
    public void Phone_Last_Digits_Preserves_Hyphens_Between_Masked_Digit_Groups()
    {
        var r = new TextRedactor(PrivacyPolicies.Minimal(b => b.AddRule(new PhoneRedactionRule(PhoneMaskMode.LastDigits, minDigits: 10))));
        var res = r.Redact("123-456-7890");
        Assert.NotNull(res.Text);
        Assert.Equal("***-***-7890", res.Text);
    }
}