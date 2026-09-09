using Lyo.Privacy.Abstractions;
using Lyo.Privacy.Enums;
using Lyo.Privacy.Json;
using Lyo.Privacy.Policy;

namespace Lyo.Privacy.Configuration;

/// <summary>Options bound for DI (section name <c>Privacy</c> in ASP.NET Core).</summary>
public sealed class PrivacyRedactorOptions
{
    public const string SectionName = "Privacy";

    public string Placeholder { get; set; } = "[redacted]";

    /// <summary>A name from <see cref="PrivacyPresetNames" />.</summary>
    public string DefaultPreset { get; set; } = PrivacyPresetNames.Logging;

    /// <summary>
    /// Metrics tag <c>policy</c> and <see cref="RedactionResult.PolicyName" /> for text and JSON redactors. Null or whitespace falls back to
    /// <see cref="DefaultPreset" />.
    /// </summary>
    public string? PolicyName { get; set; }

    /// <summary>When true, JSON string values are piped through <see cref="ITextRedactor" />.</summary>
    public bool JsonApplyTextRulesToStrings { get; set; } = true;

    public byte[]? JsonStableHashSalt { get; set; }

    public IReadOnlyDictionary<string, JsonKeyRedactionStrategy>? JsonSensitiveKeys { get; set; }

    public RedactionPolicy BuildTextPolicy(Action<RedactionPolicyBuilder>? configure = null)
    {
        var tag = string.IsNullOrWhiteSpace(PolicyName) ? DefaultPreset : PolicyName!.Trim();
        var b = new RedactionPolicyBuilder().WithPlaceholder(Placeholder).WithPolicyName(tag);
        if (!string.Equals(DefaultPreset, PrivacyPresetNames.Minimal, StringComparison.Ordinal))
            b.AppendPreset(DefaultPreset);

        configure?.Invoke(b);
        return b.Build();
    }

    public JsonRedactorOptions BuildJsonOptions()
    {
        var tag = string.IsNullOrWhiteSpace(PolicyName) ? DefaultPreset : PolicyName!.Trim();
        return new() {
            Placeholder = Placeholder,
            StableHashSalt = JsonStableHashSalt,
            SensitiveKeys = JsonSensitiveKeys ?? JsonRedactorOptions.DefaultSensitiveKeys,
            ApplyTextRulesToAllStringValues = JsonApplyTextRulesToStrings,
            PolicyName = tag
        };
    }
}