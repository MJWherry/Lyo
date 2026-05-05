using Lyo.Privacy.Enums;
using Lyo.Privacy.Policy;
using Lyo.Privacy.Rules;

namespace Lyo.Privacy.Configuration;

/// <summary>
/// Built-in redaction presets. These helpers are for operational hygiene (logs, exports, UI), not statutory compliance frameworks (for example HIPAA Safe Harbor or GDPR
/// pseudonymisation as legal terms).
/// </summary>
public static class PrivacyPolicies
{
    /// <summary>Appends rules for a named preset to an existing builder (does not clear the builder).</summary>
    public static RedactionPolicyBuilder AppendPreset(this RedactionPolicyBuilder builder, string preset)
    {
        if (preset is null)
            throw new ArgumentNullException(nameof(preset));

        switch (preset) {
            case PrivacyPresetNames.Minimal:
                break;
            case PrivacyPresetNames.Logging:
                AddLoggingCore(builder);
                break;
            case PrivacyPresetNames.SupportExport:
                AddLoggingCore(builder);
                builder.AddRule(new RegexRedactionRule(@"(?i)\bBearer\s+[A-Za-z0-9\-._~+/]+=*", RedactionKind.Custom));
                builder.AddRule(new RegexRedactionRule(@"\beyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\b", RedactionKind.Custom));
                break;
            case PrivacyPresetNames.PublicSurface:
                builder.AddNationalIdRule(NationalIdPacks.UnitedStatesSsn);
                builder.AddRule(new EmailRedactionRule())
                    .AddRule(new PhoneRedactionRule(PhoneMaskMode.Full))
                    .AddRule(new PaymentCardRedactionRule())
                    .AddRule(new UrlRedactionRule())
                    .AddRule(new IpAddressRedactionRule())
                    .AddRule(new AddressRedactionRule());

                break;
            case PrivacyPresetNames.RegressionTesting:
                AddLoggingCore(builder);
                break;
            default:
                throw new ArgumentException($"Unknown privacy preset '{preset}'.", nameof(preset));
        }

        return builder;
    }

    private static void AddLoggingCore(RedactionPolicyBuilder builder)
        => builder.AddNationalIdRule(NationalIdPacks.UnitedStatesSsn)
            .AddRule(new EmailRedactionRule())
            .AddRule(new PhoneRedactionRule(PhoneMaskMode.LastDigits))
            .AddRule(new PaymentCardRedactionRule())
            .AddRule(new UrlRedactionRule())
            .AddRule(new IpAddressRedactionRule(IpRedactionMode.TruncateLastSegment));

    /// <summary>Only rules you add explicitly (plus optional configure callback).</summary>
    public static RedactionPolicy Minimal(Action<RedactionPolicyBuilder>? configure = null)
    {
        var b = new RedactionPolicyBuilder().WithPolicyName(PrivacyPresetNames.Minimal);
        configure?.Invoke(b);
        return b.Build();
    }

    /// <summary>Typical APM / log shipping: email, partial phone, cards, URL queries, truncated IPv4.</summary>
    public static RedactionPolicy Logging(Action<RedactionPolicyBuilder>? configure = null)
    {
        var b = new RedactionPolicyBuilder().WithPolicyName(PrivacyPresetNames.Logging);
        b.AppendPreset(PrivacyPresetNames.Logging);
        configure?.Invoke(b);
        return b.Build();
    }

    /// <summary>Tier-1 artifacts: logging core plus bearer tokens and JWT-shaped strings.</summary>
    public static RedactionPolicy SupportExport(Action<RedactionPolicyBuilder>? configure = null)
    {
        var b = new RedactionPolicyBuilder().WithPolicyName(PrivacyPresetNames.SupportExport);
        b.AppendPreset(PrivacyPresetNames.SupportExport);
        configure?.Invoke(b);
        return b.Build();
    }

    /// <summary>End-user visible strings: full IP and phone masking, plus financial/URL/email patterns.</summary>
    public static RedactionPolicy PublicSurface(Action<RedactionPolicyBuilder>? configure = null)
    {
        var b = new RedactionPolicyBuilder().WithPolicyName(PrivacyPresetNames.PublicSurface);
        b.AppendPreset(PrivacyPresetNames.PublicSurface);
        configure?.Invoke(b);
        return b.Build();
    }

    /// <summary>Deterministic placeholder for snapshot tests.</summary>
    public static RedactionPolicy RegressionTesting(Action<RedactionPolicyBuilder>? configure = null)
    {
        var b = new RedactionPolicyBuilder().WithPlaceholder("[REDACTED]").WithPolicyName(PrivacyPresetNames.RegressionTesting);
        b.AppendPreset(PrivacyPresetNames.RegressionTesting);
        configure?.Invoke(b);
        return b.Build();
    }

    /// <summary>Builds a policy from a preset name and optional extra configuration.</summary>
    public static RedactionPolicy FromPreset(string preset, Action<RedactionPolicyBuilder>? configure = null)
    {
        var b = new RedactionPolicyBuilder().WithPolicyName(preset);
        if (preset != PrivacyPresetNames.Minimal)
            b.AppendPreset(preset);

        configure?.Invoke(b);
        return b.Build();
    }
}