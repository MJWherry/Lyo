using Lyo.Privacy.Configuration;
using Lyo.Privacy.Enums;
using Lyo.Privacy.Policy;
using Lyo.Privacy.Rules;
using Lyo.Privacy.Text;

namespace Lyo.Privacy.Tests;

public sealed class TextRedactorTests
{
    [Fact]
    public void Redact_email_replaces_with_placeholder()
    {
        var r = new TextRedactor(PrivacyPolicies.Minimal(b => b.AddRule(new EmailRedactionRule())));
        var res = r.Redact("Contact alice@example.com today");
        Assert.Equal("Contact [redacted] today", res.Text);
        Assert.Equal(1, res.CountsByKind[RedactionKind.Email]);
    }

    [Fact]
    public void Redact_url_strips_query_only()
    {
        var r = new TextRedactor(PrivacyPolicies.Minimal(b => b.AddRule(new UrlRedactionRule())));
        var res = r.Redact("See https://api.example/v1?id=1&token=secret");
        Assert.Equal("See https://api.example/v1[redacted]", res.Text);
        Assert.Equal(1, res.CountsByKind[RedactionKind.Url]);
    }

    [Fact]
    public void Redact_luhn_card_keeps_last_four()
    {
        var r = new TextRedactor(PrivacyPolicies.Minimal(b => b.AddRule(new PaymentCardRedactionRule())));
        var res = r.Redact("PAN 4111111111111111 ok");
        Assert.Equal("PAN [redacted]1111 ok", res.Text);
        Assert.Equal(1, res.CountsByKind[RedactionKind.PaymentCard]);
    }

    [Fact]
    public void Redact_non_luhn_digit_run_not_touched()
    {
        var r = new TextRedactor(PrivacyPolicies.Minimal(b => b.AddRule(new PaymentCardRedactionRule())));
        var res = r.Redact("id 12345678901234");
        Assert.Equal("id 12345678901234", res.Text);
        Assert.Equal(0, res.TotalRuns);
    }

    [Fact]
    public void Redact_ipv4_truncate_mode_only_masks_last_octet()
    {
        var r = new TextRedactor(PrivacyPolicies.Minimal(b => b.AddRule(new IpAddressRedactionRule(IpRedactionMode.TruncateLastSegment))));
        var res = r.Redact("host 203.0.113.44");
        Assert.Equal("host 203.0.113.[redacted]", res.Text);
        Assert.Equal(1, res.CountsByKind[RedactionKind.IpAddress]);
    }

    [Fact]
    public void Redact_literal_case_insensitive()
    {
        var r = new TextRedactor(PrivacyPolicies.Minimal(b => b.AddRule(new LiteralSubstringRedactionRule("ACME Corp", true))));
        var res = r.Redact("acme corp and Acme Corp");
        Assert.Equal("[redacted] and [redacted]", res.Text);
        Assert.Equal(2, res.CountsByKind[RedactionKind.Literal]);
    }

    [Fact]
    public void Redact_merge_adjacent_false_emits_per_char_for_same_run()
    {
        var policy = new RedactionPolicyBuilder().WithMergeAdjacentRuns(false).AddRule(new EmailRedactionRule()).Build();
        var r = new TextRedactor(policy);
        var res = r.Redact("x@y.co");
        var expectedLen = "x@y.co".Length;
        var expected = string.Concat(Enumerable.Repeat("[redacted]", expectedLen));
        Assert.Equal(expected, res.Text);
    }

    [Fact]
    public void Earlier_rule_wins_overlap()
    {
        var policy = new RedactionPolicyBuilder().AddRule(new LiteralSubstringRedactionRule("foo@bar.com")).AddRule(new EmailRedactionRule()).Build();
        var r = new TextRedactor(policy);
        var res = r.Redact("hello foo@bar.com end");
        Assert.Equal("hello [redacted] end", res.Text);
        Assert.Equal(1, res.CountsByKind[RedactionKind.Literal]);
    }

    [Fact]
    public void Null_input_yields_null_text()
    {
        var r = new TextRedactor(PrivacyPolicies.Logging());
        var res = r.Redact(null);
        Assert.Null(res.Text);
        Assert.Empty(res.CountsByKind);
    }

    [Fact]
    public void Phone_last_digits_keeps_suffix()
    {
        var r = new TextRedactor(PrivacyPolicies.Minimal(b => b.AddRule(new PhoneRedactionRule(PhoneMaskMode.LastDigits, minDigits: 10))));
        var res = r.Redact("call +1-555-123-4567");
        Assert.NotNull(res.Text);
        Assert.Contains("4567", res.Text);
        Assert.Contains("*", res.Text);
        Assert.DoesNotContain("[redacted]", res.Text);
    }
}