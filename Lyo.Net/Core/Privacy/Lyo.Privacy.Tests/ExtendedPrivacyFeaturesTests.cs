using System.Text;
using System.Text.Json;
using Lyo.Privacy.Configuration;
using Lyo.Privacy.Enums;
using Lyo.Privacy.Json;
using Lyo.Privacy.Policy;
using Lyo.Privacy.Rules;
using Lyo.Privacy.Text;
using Lyo.Privacy.Xml;

namespace Lyo.Privacy.Tests;

public sealed class ExtendedPrivacyFeaturesTests
{
    [Fact]
    public void Iban_redacts_when_mod97_valid()
    {
        var r = new TextRedactor(PrivacyPolicies.Minimal(b => b.AddRule(new IbanRedactionRule())));
        var iban = "DE89370400440532013000";
        var res = r.Redact($"Pay {iban} today");
        Assert.DoesNotContain("89370400", res.Text);
        Assert.Contains("[redacted]", res.Text);
        Assert.Equal(1, res.CountsByKind[RedactionKind.Iban]);
    }

    [Fact]
    public void Iban_skips_invalid_checksum()
    {
        var r = new TextRedactor(PrivacyPolicies.Minimal(b => b.AddRule(new IbanRedactionRule())));
        var iban = "DE89370400440532013001";
        var res = r.Redact($"Pay {iban} today");
        Assert.Contains(iban, res.Text);
    }

    [Fact]
    public void Bank_account_heuristic_masks_long_digit_run()
    {
        var r = new TextRedactor(PrivacyPolicies.Minimal(b => b.AddRule(new BankAccountNumberRedactionRule())));
        var res = r.Redact("acct 12345678901234 ref");
        Assert.DoesNotContain("12345678901234", res.Text);
        Assert.Equal(1, res.CountsByKind[RedactionKind.BankAccountNumber]);
    }

    [Fact]
    public void Bank_account_min_numeric_filters_small_numbers()
    {
        var rule = new BankAccountNumberRedactionRule { MinNumericValue = 1_000_000_000 };
        var r = new TextRedactor(PrivacyPolicies.Minimal(b => b.AddRule(rule)));
        var res = r.Redact("id 12345678");
        Assert.Contains("12345678", res.Text);
    }

    [Fact]
    public void ApiSecret_aws_and_github_and_assignment()
    {
        var rule = new ApiSecretRedactionRule(ApiSecretPatterns.AwsAccessKey | ApiSecretPatterns.GitHubPersonalAccessToken | ApiSecretPatterns.HighEntropyAssignment);
        var r = new TextRedactor(PrivacyPolicies.Minimal(b => b.AddRule(rule)));
        var aws = "AKIAIOSFODNN7EXAMPLE";
        var gh = "ghp_" + new string('a', 36);
        var assign = "API_KEY=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(new string('x', 24)));
        var text = $"{aws} {gh} {assign}";
        var res = r.Redact(text);
        Assert.DoesNotContain("AKIA", res.Text);
        Assert.DoesNotContain("ghp_", res.Text);
        Assert.DoesNotContain("API_KEY=", res.Text);
        Assert.True(res.CountsByKind[RedactionKind.ApiSecret] >= 3);
    }

    [Fact]
    public void ApiSecret_entropy_gate_skips_low_entropy_assignment()
    {
        var rule = new ApiSecretRedactionRule(ApiSecretPatterns.HighEntropyAssignment, 4.0);
        var r = new TextRedactor(PrivacyPolicies.Minimal(b => b.AddRule(rule)));
        var res = r.Redact("API_KEY=aaaaaaaaaaaaaaaa");
        Assert.Contains("API_KEY=", res.Text);
        Assert.Equal(0, res.CountsByKind.GetValueOrDefault(RedactionKind.ApiSecret));
    }

    [Fact]
    public void National_id_US_pack_matches_ssn_shape()
    {
        var r = new TextRedactor(PrivacyPolicies.Minimal(b => b.AddRule(new NationalIdRedactionRule(NationalIdPacks.UnitedStatesSsn))));
        var res = r.Redact("ssn 859-98-6787 end");
        Assert.DoesNotContain("859-98-6787", res.Text);
        Assert.Equal(1, res.CountsByKind[RedactionKind.TaxId]);
    }

    [Fact]
    public void Payment_card_allowed_bin_only_redacts_matching_prefix()
    {
        var rule = new PaymentCardRedactionRule { AllowedBin6 = ["411111"] };
        var r = new TextRedactor(PrivacyPolicies.Minimal(b => b.AddRule(rule)));
        var visa = "4111111111111111";
        var mc = "5555555555554444";
        var res = r.Redact($"{visa} {mc}");
        Assert.Contains("[redacted]", res.Text);
        Assert.Contains("4444", res.Text);
        Assert.Equal(1, res.CountsByKind[RedactionKind.PaymentCard]);
    }

    [Fact]
    public void Payment_card_blocked_bin_skips()
    {
        var rule = new PaymentCardRedactionRule { BlockedBin6 = ["411111"] };
        var r = new TextRedactor(PrivacyPolicies.Minimal(b => b.AddRule(rule)));
        var res = r.Redact("PAN 4111111111111111");
        Assert.Contains("4111111111111111", res.Text);
    }

    [Fact]
    public void Never_redact_literals_preserve_substrings()
    {
        var policy = new RedactionPolicyBuilder().AddRule(new EmailRedactionRule()).WithNeverRedactSubstrings(["safe@staging.local"]).Build();
        var r = new TextRedactor(policy);
        var res = r.Redact("mail safe@staging.local and alice@x.com");
        Assert.Contains("safe@staging.local", res.Text);
        Assert.DoesNotContain("alice@x.com", res.Text);
    }

    [Fact]
    public void Text_redactor_span_overload_matches_string_path()
    {
        var policy = new RedactionPolicyBuilder().AddRule(new EmailRedactionRule()).Build();
        var r = new TextRedactor(policy);
        var s = "x a@b.co y";
        var a = r.Redact(s);
        var span = s.AsSpan();
        var b = r.Redact(span);
        Assert.Equal(a.Text, b.Text);
    }

    [Fact]
    public void Json_redact_utf8_parses_without_string_constructor_on_caller()
    {
        var keys = new Dictionary<string, JsonKeyRedactionStrategy>(StringComparer.OrdinalIgnoreCase) { ["password"] = JsonKeyRedactionStrategy.Placeholder };
        var opts = new JsonRedactorOptions { ApplyTextRulesToAllStringValues = false, SensitiveKeys = keys };
        var r = new JsonRedactor(opts);
        var utf8 = """{"password":"x"}"""u8.ToArray();
        var res = r.RedactJsonUtf8(utf8);
        using var doc = JsonDocument.Parse(res.Text!);
        Assert.Equal("[redacted]", doc.RootElement.GetProperty("password").GetString());
    }

    [Fact]
    public void Json_redact_stream_round_trips_utf8()
    {
        var keys = new Dictionary<string, JsonKeyRedactionStrategy>(StringComparer.OrdinalIgnoreCase) { ["token"] = JsonKeyRedactionStrategy.Placeholder };
        var r = new JsonRedactor(new() { SensitiveKeys = keys });
        using var input = new MemoryStream("""{"token":"secret"}"""u8.ToArray());
        using var output = new MemoryStream();
        r.RedactJsonStream(input, output);
        var text = Encoding.UTF8.GetString(output.ToArray());
        Assert.Contains("[redacted]", text, StringComparison.Ordinal);
    }

    [Fact]
    public void PolicyJson_builds_expected_rules()
    {
        const string json = """
                            {
                              "placeholder": "■",
                              "name": "from-json",
                              "neverRedactSubstrings": ["STAGING_MARKER"],
                              "rules": [
                                { "kind": "iban" },
                                { "kind": "apiSecret", "apiPatterns": ["aws"], "apiMinEntropy": 0 },
                                { "kind": "paymentCard", "allowedBins": ["411237"] }
                              ]
                            }
                            """;

        var p = PolicyJson.Build(json);
        Assert.Equal("■", p.Placeholder);
        Assert.Equal("from-json", p.Name);
        Assert.Contains("STAGING_MARKER", p.NeverRedactSubstrings);
        Assert.Equal(3, p.Rules.Count);
    }

    [Fact]
    public void Redaction_policy_fingerprint_changes_with_placeholder_not_secrets()
    {
        var a = new RedactionPolicyBuilder().WithPlaceholder("a").AddRule(new EmailRedactionRule()).Build();
        var b = new RedactionPolicyBuilder().WithPlaceholder("b").AddRule(new EmailRedactionRule()).Build();
        Assert.NotEqual(RedactionPolicyFingerprint.ComputeSha256HexPrefix(a), RedactionPolicyFingerprint.ComputeSha256HexPrefix(b));
        var a2 = new RedactionPolicyBuilder().WithPlaceholder("a").AddRule(new EmailRedactionRule()).Build();
        Assert.Equal(RedactionPolicyFingerprint.ComputeSha256HexPrefix(a), RedactionPolicyFingerprint.ComputeSha256HexPrefix(a2));
    }

    [Fact]
    public void Xml_redactor_placeholders_sensitive_element()
    {
        var x = new XmlRedactor(new());
        var res = x.RedactXml("<r><password>secret</password></r>");
        Assert.DoesNotContain("secret", res.Text);
        Assert.Equal(1, res.CountsByKind[RedactionKind.XmlSensitive]);
    }

    [Fact]
    public void Xml_redactor_invalid_xml_falls_back_to_text()
    {
        var text = new TextRedactor(PrivacyPolicies.Minimal(b => b.AddRule(new EmailRedactionRule())));
        var x = new XmlRedactor(new(), text);
        var res = x.RedactXml("<BAD email=\"a@b.co\"");
        Assert.DoesNotContain("@", res.Text);
    }

    [Fact]
    public void Phone_mask_options_factory_last_four()
    {
        var o = PhoneMaskOptions.LastFourDigits(true);
        var r = new TextRedactor(PrivacyPolicies.Minimal(b => b.AddRule(new PhoneRedactionRule(o))));
        var res = r.Redact("Call 5551234567");
        Assert.EndsWith("4567", res.Text!.Trim(), StringComparison.Ordinal);
    }

    [Fact]
    public void Email_mask_options_factory_partial_local()
    {
        var o = EmailMaskOptions.PartialLocalPreserveDomain();
        var r = new TextRedactor(PrivacyPolicies.Minimal(b => b.AddRule(new EmailRedactionRule(o))));
        var res = r.Redact("alice@example.com");
        Assert.Contains("@example.com", res.Text);
        Assert.StartsWith("a", res.Text, StringComparison.Ordinal);
    }
}