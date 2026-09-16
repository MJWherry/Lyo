using Lyo.Exceptions;

namespace Lyo.FeatureFlags;

/// <summary>Named flags bound from the <see cref="SectionName" /> configuration section or built with <see cref="FeatureFlagOptionExtensions.Add" />.</summary>
public sealed class FeatureFlagOptions
{
    /// <summary>Configuration section <c>AddFeatureFlagsFromConfiguration</c> binds by default.</summary>
    public const string SectionName = "FeatureFlags";

    /// <summary>Flags keyed by name. <see cref="Validate" /> rebuilds this with an ordinal-ignore-case comparer.</summary>
    public Dictionary<string, FeatureFlagDefinition> Flags { get; set; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Checks names, modes, percentages, and rules, then stores <see cref="Flags" /> with an ordinal-ignore-case comparer.
    /// Call this before <see cref="FeatureFlagService" /> reads the options. Registration does that.
    /// </summary>
    public void Validate()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in Flags.Keys)
        {
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
            ArgumentHelpers.ThrowIf(!seen.Add(key), $"Flag '{key}' is defined more than once.");
        }

        var normalized = new Dictionary<string, FeatureFlagDefinition>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in Flags)
        {
            var definition = pair.Value;
            ArgumentHelpers.ThrowIfNull(definition);
            definition.Name = pair.Key;
            definition.Rules ??= [];
            ValidateDefinition(definition);
            normalized[pair.Key] = definition;
        }

        Flags = normalized;
    }

    private static void ValidateDefinition(FeatureFlagDefinition definition)
    {
        var rules = definition.Rules;
        foreach (var rule in rules)
        {
            ArgumentHelpers.ThrowIfNull(rule);
            rule.Claims ??= [];
            rule.Attributes ??= [];
            ValidateRule(rule);
        }

        if (definition.Mode == FeatureFlagMode.Percentage)
        {
            ArgumentHelpers.ThrowIf(definition.Percentage is null || definition.Percentage < 0 || definition.Percentage > 100, "Percentage must be from 0 through 100.");
            ArgumentHelpers.ThrowIf(
                definition.Stickiness == FeatureFlagStickiness.Attribute && string.IsNullOrWhiteSpace(definition.StickinessAttribute),
                "StickinessAttribute is required when stickiness is Attribute.");
        }
        else
            ArgumentHelpers.ThrowIf(definition.Percentage is not null, "Percentage is only valid when mode is Percentage.");

        if (definition.Mode == FeatureFlagMode.Rules)
            ArgumentHelpers.ThrowIf(rules.Count == 0, "Rules mode requires at least one rule.");
        else
            ArgumentHelpers.ThrowIf(rules.Count > 0, "Rules are only valid when mode is Rules.");
    }

    private static void ValidateRule(FeatureFlagRule rule)
    {
        if (rule.UserId is not null)
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(rule.UserId);
        if (rule.TenantId is not null)
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(rule.TenantId);

        var predicates = (rule.UserId is not null ? 1 : 0) + (rule.TenantId is not null ? 1 : 0);
        foreach (var claim in rule.Claims)
        {
            ArgumentHelpers.ThrowIfNull(claim);
            ArgumentHelpers.ThrowIf(string.IsNullOrWhiteSpace(claim.Type) || string.IsNullOrWhiteSpace(claim.Value), "A claim rule needs both a type and a value.");
            predicates++;
        }

        foreach (var attribute in rule.Attributes)
        {
            ArgumentHelpers.ThrowIfNull(attribute);
            ArgumentHelpers.ThrowIf(string.IsNullOrWhiteSpace(attribute.Key) || string.IsNullOrWhiteSpace(attribute.Value), "An attribute rule needs both a key and a value.");
            predicates++;
        }

        ArgumentHelpers.ThrowIf(predicates == 0, "A rule needs a user, tenant, claim, or attribute.");
    }
}
