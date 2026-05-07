using System.Text.Json;
using System.Text.Json.Serialization;
using Lyo.Exceptions;
using Lyo.Privacy.Abstractions;
using Lyo.Privacy.Enums;
using Lyo.Privacy.Rules;

namespace Lyo.Privacy.Policy;

/// <summary>Deserialize policy JSON into <see cref="RedactionPolicyBuilder" /> rules, and serialize <see cref="RedactionPolicy" /> to the same shape.</summary>
public static class PolicyJson
{
    private static readonly JsonSerializerOptions JsonOpts = new() {
        PropertyNameCaseInsensitive = true, ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true
    };

    private static readonly JsonSerializerOptions WriteOpts = new() {
        WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>Build a policy from JSON. Optional callback mutates the builder before <see cref="RedactionPolicyBuilder.Build" />.</summary>
    public static RedactionPolicy Build(string json, Action<RedactionPolicyBuilder>? configure = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(json);
        var dto = JsonSerializer.Deserialize<PolicyDefinitionDto>(json, JsonOpts) ?? throw new JsonException("Policy JSON deserialized to null.");
        var b = new RedactionPolicyBuilder();
        if (!string.IsNullOrEmpty(dto.Placeholder))
            b.WithPlaceholder(dto.Placeholder!);

        if (!string.IsNullOrEmpty(dto.Name))
            b.WithPolicyName(dto.Name);

        if (dto.MergeAdjacentRuns is { } mar)
            b.WithMergeAdjacentRuns(mar);

        if (dto.NeverRedactSubstrings is { Count: > 0 } nr)
            b.WithNeverRedactSubstrings(nr);

        foreach (var r in dto.Rules ?? [])
            b.AddRule(CreateRule(r));

        configure?.Invoke(b);
        return b.Build();
    }

    /// <summary>Serializes a policy to JSON suitable for <see cref="Build" />. Rules must be concrete types this library can map; <see cref="DelegateRedactionRule" /> is not supported.</summary>
    public static string SerializePolicy(RedactionPolicy policy, bool writeIndented = true)
    {
        ArgumentHelpers.ThrowIfNull(policy);
        var opts = writeIndented ? WriteOpts : new(WriteOpts) { WriteIndented = false };
        return JsonSerializer.Serialize(ToDefinitionDto(policy), opts);
    }

    /// <summary>Builds the JSON DTO for <see cref="SerializePolicy" /> without stringifying.</summary>
    public static PolicyDefinitionDto ToDefinitionDto(RedactionPolicy policy)
    {
        ArgumentHelpers.ThrowIfNull(policy);
        var rules = new List<PolicyRuleDto>();
        foreach (var r in policy.Rules)
            AppendRuleDtos(rules, r);

        return new() {
            Placeholder = policy.Placeholder,
            Name = policy.Name,
            MergeAdjacentRuns = policy.MergeAdjacentRuns,
            NeverRedactSubstrings = policy.NeverRedactSubstrings.Count == 0 ? null : policy.NeverRedactSubstrings.ToList(),
            Rules = rules.Count == 0 ? null : rules
        };
    }

    private static void AppendRuleDtos(ICollection<PolicyRuleDto> rules, IRedactionRule rule)
    {
        switch (rule) {
            case CompositeRedactionRule c:
                foreach (var inner in c.InnerRules)
                    AppendRuleDtos(rules, inner);

                break;
            case DelegateRedactionRule:
                // ReSharper disable once UseStringInterpolation
                throw new InvalidOperationException(typeof(DelegateRedactionRule).Name + " cannot be exported to policy JSON.");
            case EmailRedactionRule e:
                rules.Add(new() { Kind = "EMAIL", EmailOptions = e.Options });
                break;
            case PhoneRedactionRule p:
                rules.Add(new() { Kind = "PHONE", PhoneMask = p.MaskOptions, PhoneMinDigits = p.MinDigits });
                break;
            case PaymentCardRedactionRule card:
                rules.Add(
                    new() {
                        Kind = "CARD",
                        AllowedBins = card.AllowedBin6 is { Count: > 0 } ? card.AllowedBin6.OrderBy(s => s, StringComparer.Ordinal).ToList() : null,
                        BlockedBins = card.BlockedBin6 is { Count: > 0 } ? card.BlockedBin6.OrderBy(s => s, StringComparer.Ordinal).ToList() : null
                    });

                break;
            case UrlRedactionRule:
                rules.Add(new() { Kind = "URL" });
                break;
            case IpAddressRedactionRule ip:
                rules.Add(new() { Kind = "IP", IpMode = ip.Mode == IpRedactionMode.TruncateLastSegment ? "truncate" : "full" });
                break;
            case AddressRedactionRule:
                rules.Add(new() { Kind = "ADDRESS" });
                break;
            case IbanRedactionRule:
                rules.Add(new() { Kind = "IBAN" });
                break;
            case BankAccountNumberRedactionRule bank:
                rules.Add(new() { Kind = "BANK", BankMinNumeric = bank.MinNumericValue == 0 ? null : bank.MinNumericValue });
                break;
            case NationalIdRedactionRule n:
                rules.Add(new() { Kind = "NATIONALID", NationalIdPacks = NationalPacksToDtoList(n.Packs) });
                break;
            case ApiSecretRedactionRule a:
                rules.Add(
                    new() { Kind = "APISECRET", ApiPatterns = ApiPatternsToDtoList(a.Patterns), ApiMinEntropy = a.MinEntropyBitsPerChar <= 0 ? null : a.MinEntropyBitsPerChar });

                break;
            case LiteralSubstringRedactionRule lit:
                rules.Add(new() { Kind = "LITERAL", Literal = lit.Needle, LiteralIgnoreCase = lit.IgnoreCase });
                break;
            case RegexRedactionRule rx:
                rules.Add(new() { Kind = "REGEX", Regex = rx.Pattern, RegexKind = rx.Kind.ToString() });
                break;
            default:
                throw new InvalidOperationException($"Cannot export rule type {rule.GetType().Name} to policy JSON.");
        }
    }

    private static List<string> NationalPacksToDtoList(NationalIdPacks packs)
    {
        var list = new List<string>();
        if ((packs & NationalIdPacks.UnitedStatesSsn) != 0)
            list.Add("US_SSN");

        if ((packs & NationalIdPacks.UnitedKingdomNino) != 0)
            list.Add("UK_NINO");

        if ((packs & NationalIdPacks.GermanySteuerId) != 0)
            list.Add("DE_STEUER");

        if (list.Count == 0)
            throw new InvalidOperationException("NationalId rule has no packs set.");

        return list;
    }

    private static List<string> ApiPatternsToDtoList(ApiSecretPatterns patterns)
    {
        var list = new List<string>();
        if ((patterns & ApiSecretPatterns.AwsAccessKey) != 0)
            list.Add("AWS");

        if ((patterns & ApiSecretPatterns.GitHubPersonalAccessToken) != 0)
            list.Add("GITHUB");

        if ((patterns & ApiSecretPatterns.HighEntropyAssignment) != 0)
            list.Add("ASSIGNMENT");

        if (list.Count == 0)
            throw new InvalidOperationException("ApiSecret rule has no patterns set.");

        return list;
    }

    private static IRedactionRule CreateRule(PolicyRuleDto dto)
    {
        switch (dto.Kind.Trim().ToUpperInvariant()) {
            case "EMAIL":
                if (dto.EmailOptions is not null)
                    return new EmailRedactionRule(dto.EmailOptions);

                var em = (dto.EmailMask ?? "placeholder").ToUpperInvariant();
                return em switch {
                    "PLACEHOLDER" or "POLICY" => new EmailRedactionRule(),
                    "PARTIAL_PRESERVE" or "PARTIALLOCAL" => new EmailRedactionRule(EmailMaskStyle.PartialLocalPreserveDomain, dto.EmailLocalPrefix ?? 1),
                    "PARTIAL_MASK_DOMAIN" or "MASKDOMAIN" => new EmailRedactionRule(EmailMaskStyle.PartialLocalMaskDomain, dto.EmailLocalPrefix ?? 1),
                    var _ => throw new JsonException($"Unknown emailMask '{dto.EmailMask}'.")
                };
            case "PHONE":
                if (dto.PhoneMask is not null)
                    return new PhoneRedactionRule(dto.PhoneMask, dto.PhoneMinDigits ?? 10);

                var pm = (dto.PhoneMode ?? "lastDigits").ToUpperInvariant();
                return pm switch {
                    "FULL" or "PLACEHOLDER" => new PhoneRedactionRule(PhoneMaskMode.Full),
                    "LASTDIGITS" or "LAST" => new PhoneRedactionRule(PhoneMaskMode.LastDigits, dto.PhoneDigits ?? 4, dto.PhoneMinDigits ?? 10),
                    "FIRSTOFLAST" or "FIRSTDIGITLASTGROUP" => new PhoneRedactionRule(PhoneMaskMode.FirstDigitOfLastGroup, dto.PhoneDigits ?? 4, dto.PhoneMinDigits ?? 10),
                    var _ => throw new JsonException($"Unknown phoneMode '{dto.PhoneMode}'.")
                };
            case "PAYMENTCARD":
            case "CARD":
                return new PaymentCardRedactionRule {
                    AllowedBin6 = dto.AllowedBins is { Count: > 0 } ? new HashSet<string>(dto.AllowedBins) : null,
                    BlockedBin6 = dto.BlockedBins is { Count: > 0 } ? new HashSet<string>(dto.BlockedBins) : null
                };
            case "URL":
                return new UrlRedactionRule();
            case "IP":
            case "IPADDRESS":
                var im = (dto.IpMode ?? "full").ToUpperInvariant();
                var mode = im switch {
                    "TRUNCATE" or "TRUNCATELAST" => IpRedactionMode.TruncateLastSegment,
                    var _ => IpRedactionMode.Full
                };

                return new IpAddressRedactionRule(mode);
            case "ADDRESS":
                return new AddressRedactionRule();
            case "IBAN":
                return new IbanRedactionRule();
            case "BANKACCOUNT":
            case "BANK":
                return new BankAccountNumberRedactionRule { MinNumericValue = dto.BankMinNumeric ?? 0 };
            case "NATIONALID":
            case "TAXID":
                return new NationalIdRedactionRule(ParseNationalPacks(dto.NationalIdPacks));
            case "APISECRET":
            case "SECRET":
                return new ApiSecretRedactionRule(ParseApiPatterns(dto.ApiPatterns), dto.ApiMinEntropy ?? 0);
            case "LITERAL":
                if (string.IsNullOrEmpty(dto.Literal))
                    throw new JsonException("Literal rule requires 'literal'.");

                return new LiteralSubstringRedactionRule(dto.Literal!, dto.LiteralIgnoreCase ?? true);
            case "REGEX":
                if (string.IsNullOrEmpty(dto.Regex))
                    throw new JsonException("Regex rule requires 'regex'.");

                var rk = Enum.TryParse<RedactionKind>(dto.RegexKind, true, out var k) ? k : RedactionKind.Regex;
                return new RegexRedactionRule(dto.Regex!, rk);
            default:
                throw new JsonException($"Unknown rule kind '{dto.Kind}'.");
        }
    }

    private static NationalIdPacks ParseNationalPacks(List<string>? list)
    {
        NationalIdPacks p = 0;
        foreach (var s in list ?? []) {
            p |= s.Trim().ToUpperInvariant().Replace(' ', '_') switch {
                "US_SSN" or "US" or "SSN" => NationalIdPacks.UnitedStatesSsn,
                "UK_NINO" or "UK" or "NINO" => NationalIdPacks.UnitedKingdomNino,
                "DE_STEUER" or "DE" or "GERMANY" or "STEUERID" => NationalIdPacks.GermanySteuerId,
                var _ => throw new JsonException($"Unknown nationalIdPack '{s}'.")
            };
        }

        if (p == NationalIdPacks.None)
            throw new JsonException("nationalIdPacks must contain at least one pack.");

        return p;
    }

    private static ApiSecretPatterns ParseApiPatterns(List<string>? list)
    {
        ApiSecretPatterns p = 0;
        foreach (var s in list ?? []) {
            p |= s.Trim().ToUpperInvariant().Replace(' ', '_') switch {
                "AWS" or "AWSACCESSKEY" => ApiSecretPatterns.AwsAccessKey,
                "GITHUB" or "GITHUBPAT" or "GH" => ApiSecretPatterns.GitHubPersonalAccessToken,
                "ASSIGNMENT" or "KEYEQUALS" or "ENV" => ApiSecretPatterns.HighEntropyAssignment,
                var _ => throw new JsonException($"Unknown apiPatterns '{s}'.")
            };
        }

        if (p == ApiSecretPatterns.None)
            throw new JsonException("apiPatterns must contain at least one pattern.");

        return p;
    }
}