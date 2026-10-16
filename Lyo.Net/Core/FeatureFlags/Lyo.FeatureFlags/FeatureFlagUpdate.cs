using Lyo.Exceptions;

namespace Lyo.FeatureFlags;

/// <summary>Builds a replacement document for <see cref="IFeatureFlagUpdater.TryApply" />. The previous document is not changed.</summary>
public static class FeatureFlagUpdate
{
    /// <summary>
    /// Copies <paramref name="current" /> and sets <paramref name="name" /> to on or off.
    /// Percentage, stickiness, and rules on that flag are cleared. Other flags are copied.
    /// Returns null when the name is missing. A null document or a whitespace name throws.
    /// </summary>
    public static FeatureFlagOptions? WithEnabled(FeatureFlagOptions current, string name, bool enabled)
    {
        ArgumentHelpers.ThrowIfNull(current);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(name);
        string? key = null;
        foreach (var existing in current.Flags.Keys)
        {
            if (!string.Equals(existing, name, StringComparison.OrdinalIgnoreCase))
                continue;

            key = existing;
            break;
        }

        if (key is null)
            return null;

        var next = new FeatureFlagOptions();
        foreach (var pair in current.Flags)
            next.Flags[pair.Key] = string.Equals(pair.Key, key, StringComparison.Ordinal) ? Enabled(pair.Key, enabled) : Copy(pair.Value);

        return next;
    }

    private static FeatureFlagDefinition Enabled(string name, bool enabled) => new()
    {
        Name = name,
        Mode = enabled ? FeatureFlagMode.On : FeatureFlagMode.Off
    };

    private static FeatureFlagDefinition Copy(FeatureFlagDefinition source)
    {
        var rules = new List<FeatureFlagRule>(source.Rules.Count);
        foreach (var rule in source.Rules)
            rules.Add(Copy(rule));

        return new FeatureFlagDefinition
        {
            Name = source.Name,
            Mode = source.Mode,
            Percentage = source.Percentage,
            Stickiness = source.Stickiness,
            StickinessAttribute = source.StickinessAttribute,
            Rules = rules
        };
    }

    private static FeatureFlagRule Copy(FeatureFlagRule source)
    {
        var claims = new List<FeatureFlagClaim>(source.Claims.Count);
        foreach (var claim in source.Claims)
            claims.Add(new FeatureFlagClaim { Type = claim.Type, Value = claim.Value });

        var attributes = new List<FeatureFlagAttribute>(source.Attributes.Count);
        foreach (var attribute in source.Attributes)
            attributes.Add(new FeatureFlagAttribute { Key = attribute.Key, Value = attribute.Value });

        return new FeatureFlagRule
        {
            UserId = source.UserId,
            TenantId = source.TenantId,
            Claims = claims,
            Attributes = attributes
        };
    }
}
