using System.Text.RegularExpressions;
using Lyo.Exceptions;
using Lyo.Privacy.Abstractions;
using Lyo.Privacy.Enums;
using Lyo.Privacy.Rules;

namespace Lyo.Privacy.Policy;

/// <summary>Fluent composition of <see cref="RedactionPolicy" /> instances.</summary>
public sealed class RedactionPolicyBuilder
{
    private readonly List<string> _neverRedact = new();
    private readonly List<IRedactionRule> _rules = new();
    private bool _mergeAdjacent = true;
    private string _placeholder = "[redacted]";
    private string? _policyName;

    public RedactionPolicyBuilder WithPlaceholder(string placeholder)
    {
        if (string.IsNullOrEmpty(placeholder))
            throw new ArgumentException("Value cannot be null or empty.", nameof(placeholder));

        _placeholder = placeholder;
        return this;
    }

    public RedactionPolicyBuilder WithMergeAdjacentRuns(bool merge = true)
    {
        _mergeAdjacent = merge;
        return this;
    }

    /// <summary>
    /// Optional name attached to <see cref="RedactionPolicy.Name" /> for metrics (tag <c>policy</c>) and <see cref="RedactionResult.PolicyName" />. Whitespace-only is treated as
    /// unset.
    /// </summary>
    public RedactionPolicyBuilder WithPolicyName(string? name)
    {
        _policyName = name?.Trim();
        return this;
    }

    /// <summary>Literal substrings that must never be masked (staging markers, etc.). Uses <see cref="StringComparison.Ordinal" />.</summary>
    public RedactionPolicyBuilder WithNeverRedactSubstrings(IEnumerable<string> literals)
    {
        ArgumentHelpers.ThrowIfNull(literals);
        foreach (var s in literals) {
            ArgumentHelpers.ThrowIfNullOrEmpty(s, "Never-redact literal cannot be null or empty.");
            _neverRedact.Add(s);
        }

        return this;
    }

    /// <summary>Add single never-redact literal.</summary>
    public RedactionPolicyBuilder AddNeverRedactSubstring(string literal)
    {
        ArgumentHelpers.ThrowIfNullOrEmpty(literal);
        _neverRedact.Add(literal);
        return this;
    }

    public RedactionPolicyBuilder AddRule(IRedactionRule rule)
    {
        ArgumentHelpers.ThrowIfNull(rule);
        _rules.Add(rule);
        return this;
    }

    /// <summary>Same as <see cref="AddRule(IRedactionRule)" />; useful when the rule type is explicit at the call site.</summary>
    public RedactionPolicyBuilder AddRule<T>(T rule)
        where T : class, IRedactionRule
        => AddRule((IRedactionRule)rule);

    public RedactionPolicyBuilder AddRules(IEnumerable<IRedactionRule> rules)
    {
        ArgumentHelpers.ThrowIfNull(rules);
        foreach (var r in rules) {
            if (r is null)
                throw new ArgumentException("Rule collection contained null.", nameof(rules));

            _rules.Add(r);
        }

        return this;
    }

    /// <summary>Appends all rules from an existing policy (placeholder and never-redact literals on this builder are unchanged).</summary>
    public RedactionPolicyBuilder AddPolicy(RedactionPolicy policy)
    {
        ArgumentHelpers.ThrowIfNull(policy);
        _rules.AddRange(policy.Rules);
        _neverRedact.AddRange(policy.NeverRedactSubstrings);
        return this;
    }

    public RedactionPolicyBuilder RemoveKind(RedactionKind kind)
    {
        _rules.RemoveAll(r => r.Kind == kind);
        return this;
    }

    /// <summary>Adds a <see cref="PhoneRedactionRule" /> using <see cref="PhoneMaskOptions" />.</summary>
    public RedactionPolicyBuilder AddPhoneRule(PhoneMaskOptions options, int minDigits = 10) => AddRule(new PhoneRedactionRule(options, minDigits));

    /// <summary>Adds a <see cref="PhoneRedactionRule" /> using legacy <see cref="PhoneMaskMode" /> (maps to <see cref="PhoneMaskOptions" />).</summary>
    public RedactionPolicyBuilder AddPhoneRule(PhoneMaskMode mode, int digitsParameter = 4, int minDigits = 10)
        => AddRule(new PhoneRedactionRule(mode, digitsParameter, minDigits));

    /// <summary>Adds an <see cref="EmailRedactionRule" /> using <see cref="EmailMaskOptions" />.</summary>
    public RedactionPolicyBuilder AddEmailRule(EmailMaskOptions options) => AddRule(new EmailRedactionRule(options));

    /// <summary>Adds an <see cref="EmailRedactionRule" /> using legacy <see cref="EmailMaskStyle" />.</summary>
    public RedactionPolicyBuilder AddEmailRule(EmailMaskStyle style = EmailMaskStyle.PolicyPlaceholder, int visibleLocalPrefixLength = 1)
        => AddRule(new EmailRedactionRule(style, visibleLocalPrefixLength));

    /// <summary>Adds a <see cref="PaymentCardRedactionRule" /> with optional BIN allow/block lists (first six digits).</summary>
    public RedactionPolicyBuilder AddPaymentCardRule(IReadOnlyCollection<string>? allowedBins6 = null, IReadOnlyCollection<string>? blockedBins6 = null)
        => AddRule(new PaymentCardRedactionRule { AllowedBin6 = ToBinSet(allowedBins6), BlockedBin6 = ToBinSet(blockedBins6) });

    /// <summary>Adds a <see cref="UrlRedactionRule" />.</summary>
    public RedactionPolicyBuilder AddUrlRule() => AddRule(new UrlRedactionRule());

    /// <summary>Adds an <see cref="IpAddressRedactionRule" />.</summary>
    public RedactionPolicyBuilder AddIpRule(IpRedactionMode mode = IpRedactionMode.Full) => AddRule(new IpAddressRedactionRule(mode));

    /// <summary>Adds an <see cref="AddressRedactionRule" />.</summary>
    public RedactionPolicyBuilder AddAddressRule() => AddRule(new AddressRedactionRule());

    /// <summary>Adds an <see cref="IbanRedactionRule" />.</summary>
    public RedactionPolicyBuilder AddIbanRule() => AddRule(new IbanRedactionRule());

    /// <summary>Adds a <see cref="BankAccountNumberRedactionRule" />.</summary>
    public RedactionPolicyBuilder AddBankAccountRule(ulong minNumericValue = 0) => AddRule(new BankAccountNumberRedactionRule { MinNumericValue = minNumericValue });

    /// <summary>Adds a <see cref="NationalIdRedactionRule" /> for the given <see cref="NationalIdPacks" />.</summary>
    public RedactionPolicyBuilder AddNationalIdRule(NationalIdPacks packs) => AddRule(new NationalIdRedactionRule(packs));

    /// <summary>Adds an <see cref="ApiSecretRedactionRule" />.</summary>
    public RedactionPolicyBuilder AddApiSecretRule(ApiSecretPatterns patterns, double minEntropyBitsPerChar = 0, int minimumAssignmentValueLength = 16)
        => AddRule(new ApiSecretRedactionRule(patterns, minEntropyBitsPerChar, minimumAssignmentValueLength));

    /// <summary>Adds a <see cref="LiteralSubstringRedactionRule" />.</summary>
    public RedactionPolicyBuilder AddLiteralRule(string needle, bool ignoreCase = true) => AddRule(new LiteralSubstringRedactionRule(needle, ignoreCase));

    /// <summary>Adds a <see cref="RegexRedactionRule" />.</summary>
    public RedactionPolicyBuilder AddRegexRule(string pattern, RedactionKind kind = RedactionKind.Regex, RegexOptions extra = RegexOptions.None)
        => AddRule(new RegexRedactionRule(pattern, kind, extra));

    public RedactionPolicy Build()
        => new(_rules.ToArray(), _placeholder, _mergeAdjacent, _policyName) { NeverRedactSubstrings = _neverRedact.Count == 0 ? Array.Empty<string>() : _neverRedact.ToArray() };

    private static HashSet<string>? ToBinSet(IReadOnlyCollection<string>? bins) => bins is null || bins.Count == 0 ? null : new(bins, StringComparer.Ordinal);
}