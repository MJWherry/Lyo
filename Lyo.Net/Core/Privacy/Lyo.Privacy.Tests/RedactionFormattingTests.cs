using Lyo.Privacy.Configuration;
using Lyo.Privacy.Enums;
using Lyo.Privacy.Rules;
using Lyo.Privacy.Text;

namespace Lyo.Privacy.Tests;

public sealed class RedactionFormattingTests
{
    [Fact]
    public void Email_partial_local_preserves_domain()
    {
        var r = new TextRedactor(PrivacyPolicies.Minimal(b => b.AddRule(new EmailRedactionRule(EmailMaskStyle.PartialLocalPreserveDomain))));
        var res = r.Redact("x alice.wonder@example.com y");
        Assert.Equal("x a***@example.com y", res.Text);
    }

    [Fact]
    public void Email_partial_masks_domain_suffix()
    {
        var r = new TextRedactor(PrivacyPolicies.Minimal(b => b.AddRule(new EmailRedactionRule(EmailMaskStyle.PartialLocalMaskDomain))));
        var res = r.Redact("x u@mail.example.co.uk z");
        Assert.Equal("x u***@***.example.co.uk z", res.Text);
    }

    [Fact]
    public void Phone_first_digit_of_last_group_is_digits_only()
    {
        var r = new TextRedactor(PrivacyPolicies.Minimal(b => b.AddRule(new PhoneRedactionRule(PhoneMaskMode.FirstDigitOfLastGroup, minDigits: 10))));
        var res = r.Redact("call +1-555-123-4567");
        Assert.Equal("call *******4***", res.Text);
    }

    [Fact]
    public void Phone_options_leading_and_trailing_digit_counts_union_digits_only()
    {
        var opts = new PhoneMaskOptions { LeadingDigitsVisible = 1, TrailingDigitsVisible = 2, DigitsOnlyOutput = true };
        var r = new TextRedactor(PrivacyPolicies.Minimal(b => b.AddRule(new PhoneRedactionRule(opts, 10))));
        var res = r.Redact("call +1-555-123-4567");
        Assert.Equal("call 1********67", res.Text);
    }

    [Fact]
    public void Email_options_omit_at_sign_and_preserve_domain()
    {
        var opts = new EmailMaskOptions {
            VisibleLocalPrefixLength = 1,
            PreserveEntireDomainHost = true,
            PreserveAtSign = false,
            AtReplacement = "#"
        };

        var r = new TextRedactor(PrivacyPolicies.Minimal(b => b.AddRule(new EmailRedactionRule(opts))));
        var res = r.Redact("reach alice@mail.example.com soon");
        Assert.Equal("reach a***#mail.example.com soon", res.Text);
    }

    [Fact]
    public void Email_options_visible_domain_prefix_on_first_label()
    {
        var opts = new EmailMaskOptions { VisibleLocalPrefixLength = 1, VisibleDomainPrefixLength = 2, PreserveDomainFromFirstDot = true };
        var r = new TextRedactor(PrivacyPolicies.Minimal(b => b.AddRule(new EmailRedactionRule(opts))));
        var res = r.Redact("x u@mail.example.co.uk z");
        Assert.Equal("x u***@ma***.example.co.uk z", res.Text);
    }

    [Fact]
    public void Address_rule_matches_us_street_line()
    {
        var r = new TextRedactor(PrivacyPolicies.Minimal(b => b.AddRule(new AddressRedactionRule())));
        var res = r.Redact("ship to 742 Evergreen Terrace Road soon");
        Assert.Contains("[redacted]", res.Text);
        Assert.DoesNotContain("Evergreen", res.Text);
    }
}