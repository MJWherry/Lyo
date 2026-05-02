using System.Text.Json;
using Lyo.Exceptions;
using Lyo.Privacy.Abstractions;
using Lyo.Privacy.Enums;
using Lyo.Privacy.Rules;

namespace Lyo.Privacy.Policy;

/// <summary>Deserialize policy JSON into <see cref="RedactionPolicyBuilder" /> rules.</summary>
public static class PolicyJson
{
    private static readonly JsonSerializerOptions JsonOpts = new() {
        PropertyNameCaseInsensitive = true, ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true
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