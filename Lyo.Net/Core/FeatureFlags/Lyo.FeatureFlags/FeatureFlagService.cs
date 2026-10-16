using Lyo.Exceptions;
using Microsoft.Extensions.Options;

namespace Lyo.FeatureFlags;

/// <summary>
/// Evaluates <see cref="FeatureFlagOptions" /> from an immutable snapshot. Changing the registered options instance does not change answers.
/// <see cref="IFeatureFlagUpdater.TryApply" /> and a configuration reload replace that snapshot. Readers see the old set or the new set.
/// Register it with <c>AddFeatureFlags</c>. Apps pass a <see cref="FeatureFlagContext" />; the ASP.NET package builds one from the request.
/// </summary>
public sealed class FeatureFlagService : IFeatureFlagService, IFeatureFlagUpdater, IOptionsMonitor<FeatureFlagOptions>
{
    private readonly object _gate = new();
    private readonly List<Action<FeatureFlagOptions, string?>> _listeners = [];
    private volatile Dictionary<string, Snapshot> _flags;
    private volatile FeatureFlagOptions _current;

    /// <summary>Copies <paramref name="options" /> after <see cref="FeatureFlagOptions.Validate" />.</summary>
    public FeatureFlagService(FeatureFlagOptions options)
    {
        ArgumentHelpers.ThrowIfNull(options);
        options.Validate();
        _current = options;
        _flags = Copy(options);
    }

    /// <summary>The snapshot <see cref="TryApply" /> last accepted. This follows reloads. <see cref="IOptions{FeatureFlagOptions}" /> does not.</summary>
    public FeatureFlagOptions CurrentValue => _current;

    /// <inheritdoc />
    public bool IsEnabled(string name, FeatureFlagContext? context = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(name);
        var flags = _flags;
        if (!flags.TryGetValue(name, out var flag))
            return false;

        return flag.Mode switch
        {
            FeatureFlagMode.On => true,
            FeatureFlagMode.Percentage => Percentage(flag, context),
            FeatureFlagMode.Rules => Rules(flag, context),
            _ => false
        };
    }

    /// <inheritdoc />
    public FeatureFlagOptions Get(string? name)
    {
        if (string.IsNullOrEmpty(name) || string.Equals(name, Options.DefaultName, StringComparison.Ordinal))
            return CurrentValue;

        throw new InvalidOperationException($"Named options are not supported (requested name '{name}').");
    }

    /// <summary>Registers <paramref name="listener" /> for a later swap. The listener is not called now.</summary>
    public IDisposable OnChange(Action<FeatureFlagOptions, string?> listener)
    {
        ArgumentHelpers.ThrowIfNull(listener);
        lock (_gate)
            _listeners.Add(listener);

        return new ChangeRegistration(this, listener);
    }

    /// <inheritdoc />
    public bool TryApply(FeatureFlagOptions options)
    {
        ArgumentHelpers.ThrowIfNull(options);
        try
        {
            options.Validate();
        }
        catch (ArgumentException)
        {
            return false;
        }

        var next = Copy(options);
        Action<FeatureFlagOptions, string?>[] listeners;
        lock (_gate)
        {
            _flags = next;
            _current = options;
            listeners = _listeners.ToArray();
        }

        foreach (var listener in listeners)
            listener(options, Options.DefaultName);

        return true;
    }

    private static Dictionary<string, Snapshot> Copy(FeatureFlagOptions options)
    {
        var flags = new Dictionary<string, Snapshot>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in options.Flags)
            flags[pair.Key] = Snapshot.Copy(pair.Value);

        return flags;
    }

    private static bool Percentage(Snapshot flag, FeatureFlagContext? context)
    {
        if (flag.Percentage is 0)
            return false;
        if (flag.Percentage is 100)
            return true;

        var key = StickinessKey(flag, context);
        if (key is null)
            return false;

        return FeatureFlagHash.Bucket(flag.Name, key) < flag.Percentage;
    }

    private static string? StickinessKey(Snapshot flag, FeatureFlagContext? context)
    {
        if (context?.StickinessKey is not null)
            return context.StickinessKey;

        return flag.Stickiness switch
        {
            FeatureFlagStickiness.Tenant => context?.TenantId,
            FeatureFlagStickiness.Attribute => AttributeValue(context, flag.StickinessAttribute),
            _ => context?.UserId
        };
    }

    private static string? AttributeValue(FeatureFlagContext? context, string? key)
    {
        if (context is null || string.IsNullOrEmpty(key))
            return null;

        foreach (var attribute in context.Attributes)
        {
            if (string.Equals(attribute.Key, key, StringComparison.Ordinal))
                return attribute.Value;
        }

        return null;
    }

    private static bool Rules(Snapshot flag, FeatureFlagContext? context)
    {
        if (context is null)
            return false;

        foreach (var rule in flag.Rules)
        {
            if (Matches(rule, context))
                return true;
        }

        return false;
    }

    private static bool Matches(Rule rule, FeatureFlagContext context)
    {
        if (rule.UserId is not null && !string.Equals(rule.UserId, context.UserId, StringComparison.Ordinal))
            return false;
        if (rule.TenantId is not null && !string.Equals(rule.TenantId, context.TenantId, StringComparison.Ordinal))
            return false;

        foreach (var claim in rule.Claims)
        {
            if (!HasClaim(context, claim))
                return false;
        }

        foreach (var attribute in rule.Attributes)
        {
            if (!HasAttribute(context, attribute))
                return false;
        }

        return true;
    }

    private static bool HasClaim(FeatureFlagContext context, FeatureFlagClaim required)
    {
        foreach (var claim in context.Claims)
        {
            if (string.Equals(claim.Type, required.Type, StringComparison.Ordinal) && string.Equals(claim.Value, required.Value, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private static bool HasAttribute(FeatureFlagContext context, FeatureFlagAttribute required)
    {
        foreach (var attribute in context.Attributes)
        {
            if (string.Equals(attribute.Key, required.Key, StringComparison.Ordinal) && string.Equals(attribute.Value, required.Value, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private sealed class ChangeRegistration(FeatureFlagService service, Action<FeatureFlagOptions, string?> listener) : IDisposable
    {
        public void Dispose()
        {
            lock (service._gate)
                service._listeners.Remove(listener);
        }
    }

    private sealed class Snapshot
    {
        public required string Name { get; init; }
        public required FeatureFlagMode Mode { get; init; }
        public int? Percentage { get; init; }
        public FeatureFlagStickiness Stickiness { get; init; }
        public string? StickinessAttribute { get; init; }
        public required IReadOnlyList<Rule> Rules { get; init; }

        public static Snapshot Copy(FeatureFlagDefinition definition)
        {
            var rules = new Rule[definition.Rules.Count];
            for (var i = 0; i < definition.Rules.Count; i++)
                rules[i] = Rule.Copy(definition.Rules[i]);

            return new Snapshot
            {
                Name = definition.Name,
                Mode = definition.Mode,
                Percentage = definition.Percentage,
                Stickiness = definition.Stickiness,
                StickinessAttribute = definition.StickinessAttribute,
                Rules = rules
            };
        }
    }

    private sealed class Rule
    {
        public string? UserId { get; init; }
        public string? TenantId { get; init; }
        public required IReadOnlyList<FeatureFlagClaim> Claims { get; init; }
        public required IReadOnlyList<FeatureFlagAttribute> Attributes { get; init; }

        public static Rule Copy(FeatureFlagRule rule) => new()
        {
            UserId = rule.UserId,
            TenantId = rule.TenantId,
            Claims = CopyClaims(rule.Claims),
            Attributes = CopyAttributes(rule.Attributes)
        };

        private static FeatureFlagClaim[] CopyClaims(List<FeatureFlagClaim> claims)
        {
            var copy = new FeatureFlagClaim[claims.Count];
            for (var i = 0; i < claims.Count; i++)
                copy[i] = new FeatureFlagClaim { Type = claims[i].Type, Value = claims[i].Value };
            return copy;
        }

        private static FeatureFlagAttribute[] CopyAttributes(List<FeatureFlagAttribute> attributes)
        {
            var copy = new FeatureFlagAttribute[attributes.Count];
            for (var i = 0; i < attributes.Count; i++)
                copy[i] = new FeatureFlagAttribute { Key = attributes[i].Key, Value = attributes[i].Value };
            return copy;
        }
    }
}
