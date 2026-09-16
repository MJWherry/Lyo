using Lyo.Exceptions;

namespace Lyo.FeatureFlags;

/// <summary>Adds named flags to <see cref="FeatureFlagOptions" />. The flag stays off until a mode method runs.</summary>
public static class FeatureFlagOptionExtensions
{
    /// <param name="options">Options that will be validated at registration.</param>
    extension(FeatureFlagOptions options)
    {
        /// <summary>Starts a flag named <paramref name="name" />. A second add of the same name, ignoring case, throws. Default mode is off.</summary>
        public FeatureFlagBuilder Add(string name)
        {
            ArgumentHelpers.ThrowIfNull(options);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(name);
            foreach (var key in options.Flags.Keys)
                ArgumentHelpers.ThrowIf(string.Equals(key, name, StringComparison.OrdinalIgnoreCase), $"Flag '{name}' is already defined.");

            var definition = new FeatureFlagDefinition { Name = name };
            options.Flags[name] = definition;
            return new FeatureFlagBuilder(options, definition);
        }
    }
}

/// <summary>Sets the mode, stickiness, and rules for one flag. The last mode call clears the others. The last stickiness call wins.</summary>
public sealed class FeatureFlagBuilder
{
    private readonly FeatureFlagOptions _options;
    private readonly FeatureFlagDefinition _definition;

    internal FeatureFlagBuilder(FeatureFlagOptions options, FeatureFlagDefinition definition)
    {
        _options = options;
        _definition = definition;
    }

    /// <summary>Turns the flag on and drops percentage, stickiness, and rules.</summary>
    public FeatureFlagBuilder On()
    {
        _definition.Mode = FeatureFlagMode.On;
        _definition.Percentage = null;
        _definition.Stickiness = FeatureFlagStickiness.User;
        _definition.StickinessAttribute = null;
        _definition.Rules.Clear();
        return this;
    }

    /// <summary>Rolls the flag out by <paramref name="percentage" /> and drops rules. Stickiness already set on this flag is kept.</summary>
    public FeatureFlagBuilder Percentage(int percentage)
    {
        _definition.Mode = FeatureFlagMode.Percentage;
        _definition.Percentage = percentage;
        _definition.Rules.Clear();
        return this;
    }

    /// <summary>Hashes <see cref="FeatureFlagContext.TenantId" /> for this percentage flag.</summary>
    public FeatureFlagBuilder StickToTenant()
    {
        _definition.Stickiness = FeatureFlagStickiness.Tenant;
        _definition.StickinessAttribute = null;
        return this;
    }

    /// <summary>Hashes the named caller attribute for this percentage flag. Whitespace throws.</summary>
    public FeatureFlagBuilder StickToAttribute(string name)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(name);
        _definition.Stickiness = FeatureFlagStickiness.Attribute;
        _definition.StickinessAttribute = name;
        return this;
    }

    /// <summary>Adds an OR rule that matches one user. Whitespace throws. Switches the flag to rules and drops percentage and stickiness.</summary>
    public FeatureFlagBuilder WhenUser(string userId)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(userId);
        EnsureRules();
        _definition.Rules.Add(new FeatureFlagRule { UserId = userId });
        return this;
    }

    /// <summary>Adds an OR rule that matches one tenant. Whitespace throws. Switches the flag to rules and drops percentage and stickiness.</summary>
    public FeatureFlagBuilder WhenTenant(string tenantId)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(tenantId);
        EnsureRules();
        _definition.Rules.Add(new FeatureFlagRule { TenantId = tenantId });
        return this;
    }

    /// <summary>Adds an OR rule that matches one claim. Whitespace throws. Switches the flag to rules and drops percentage and stickiness.</summary>
    public FeatureFlagBuilder WhenClaim(string type, string value)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(type);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(value);
        EnsureRules();
        _definition.Rules.Add(new FeatureFlagRule { Claims = [new FeatureFlagClaim { Type = type, Value = value }] });
        return this;
    }

    /// <summary>Adds an OR rule that matches one attribute. Whitespace throws. Switches the flag to rules and drops percentage and stickiness.</summary>
    public FeatureFlagBuilder WhenAttribute(string key, string value)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(value);
        EnsureRules();
        _definition.Rules.Add(new FeatureFlagRule { Attributes = [new FeatureFlagAttribute { Key = key, Value = value }] });
        return this;
    }

    /// <summary>Adds one AND rule. Every predicate set inside <paramref name="configure" /> must match. Switches the flag to rules and drops percentage and stickiness.</summary>
    public FeatureFlagBuilder When(Action<FeatureFlagRuleBuilder> configure)
    {
        ArgumentHelpers.ThrowIfNull(configure);
        EnsureRules();
        var rule = new FeatureFlagRule();
        configure(new FeatureFlagRuleBuilder(rule));
        _definition.Rules.Add(rule);
        return this;
    }

    /// <summary>Starts another flag on the same options.</summary>
    public FeatureFlagBuilder Add(string name) => _options.Add(name);

    private void EnsureRules()
    {
        if (_definition.Mode == FeatureFlagMode.Rules)
            return;

        _definition.Mode = FeatureFlagMode.Rules;
        _definition.Percentage = null;
        _definition.Stickiness = FeatureFlagStickiness.User;
        _definition.StickinessAttribute = null;
        _definition.Rules.Clear();
    }
}

/// <summary>Fills one AND rule. Each call adds a predicate that must match together with the others.</summary>
public sealed class FeatureFlagRuleBuilder
{
    private readonly FeatureFlagRule _rule;

    internal FeatureFlagRuleBuilder(FeatureFlagRule rule) => _rule = rule;

    /// <summary>Requires this user id.</summary>
    public FeatureFlagRuleBuilder User(string userId)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(userId);
        _rule.UserId = userId;
        return this;
    }

    /// <summary>Requires this tenant id.</summary>
    public FeatureFlagRuleBuilder Tenant(string tenantId)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(tenantId);
        _rule.TenantId = tenantId;
        return this;
    }

    /// <summary>Requires this claim. A later call adds another claim that must also match.</summary>
    public FeatureFlagRuleBuilder Claim(string type, string value)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(type);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(value);
        _rule.Claims.Add(new FeatureFlagClaim { Type = type, Value = value });
        return this;
    }

    /// <summary>Requires this attribute. A later call adds another attribute that must also match.</summary>
    public FeatureFlagRuleBuilder Attribute(string key, string value)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(value);
        _rule.Attributes.Add(new FeatureFlagAttribute { Key = key, Value = value });
        return this;
    }
}
