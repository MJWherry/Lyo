using Lyo.Privacy.Configuration;
using Lyo.Privacy.Enums;
using Lyo.Privacy.Rules;
using Lyo.Privacy.Text;

namespace Lyo.Privacy.Tests;

public sealed class RedactionFormattingTests
{
    [Fact]
    public void Email_Partial_Local_Preserves_Domain()
    {
        var r = new TextRedactor(PrivacyPolicies.Minimal(b => b.AddRule(new EmailRedactionRule(EmailMaskStyle.PartialLocalPreserveDomain))));
        var res = r.Redact("x alice.wonder@example.com y");
        Assert.Equal("x a***@example.com y", res.Text);
    }

    [Fact]
    public void Email_Partial_Masks_Domain_Suffix()
    {
        var r = new TextRedactor(PrivacyPolicies.Minimal(b => b.AddRule(new EmailRedactionRule(EmailMaskStyle.PartialLocalMaskDomain))));
        var res = r.Redact("x u@mail.example.co.uk z");
        Assert.Equal("x u***@***.example.co.uk z", res.Text);
    }

    [Fact]
    public void Phone_First_Digit_Of_Last_Group_Is_Digits_Only()
    {
        var r = new TextRedactor(PrivacyPolicies.Minimal(b => b.AddRule(new PhoneRedactionRule(PhoneMaskMode.FirstDigitOfLastGroup, minDigits: 10))));
        var res = r.Redact("call +1-555-123-4567");
        Assert.Equal("call *******4***", res.Text);
    }

    [Fact]
    public void Phone_Options_Leading_And_Trailing_Digit_Counts_Union_Digits_Only()
    {
        var opts = new PhoneMaskOptions { LeadingDigitsVisible = 1, TrailingDigitsVisible = 2, DigitsOnlyOutput = true };
        var r = new TextRedactor(PrivacyPolicies.Minimal(b => b.AddRule(new PhoneRedactionRule(opts))));
        var res = r.Redact("call +1-555-123-4567");
        Assert.Equal("call 1********67", res.Text);
    }

    [Fact]
    public void Email_Options_Omit_At_Sign_And_Preserve_Domain()
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
    public void Email_Options_Visible_Domain_Prefix_On_First_Label()
    {
        var opts = new EmailMaskOptions { VisibleLocalPrefixLength = 1, VisibleDomainPrefixLength = 2, PreserveDomainFromFirstDot = true };
        var r = new TextRedactor(PrivacyPolicies.Minimal(b => b.AddRule(new EmailRedactionRule(opts))));
        var res = r.Redact("x u@mail.example.co.uk z");
        Assert.Equal("x u***@ma***.example.co.uk z", res.Text);
    }

    [Fact]
    public void Address_Rule_Matches_Us_Street_Line()
    {
        var r = new TextRedactor(PrivacyPolicies.Minimal(b => b.AddRule(new AddressRedactionRule())));
        var res = r.Redact("ship to 742 Evergreen Terrace Road soon");
        Assert.Contains("[redacted]", res.Text);
        Assert.DoesNotContain("Evergreen", res.Text);
    }
}