using Lyo.Exceptions;

namespace Lyo.FeatureFlags;

/// <summary>Caller values a flag evaluates. Apps build one with <see cref="Create" />. HTTP hosts map a request into one.</summary>
public sealed class FeatureFlagContext
{
    /// <summary>User id compared by rules and hashed when stickiness is <see cref="FeatureFlagStickiness.User" />.</summary>
    public string? UserId { get; }

    /// <summary>Tenant id compared by rules and hashed when stickiness is <see cref="FeatureFlagStickiness.Tenant" />.</summary>
    public string? TenantId { get; }

    /// <summary>Replaces the flag's stickiness value for a percentage of 1 through 99. On and Rules ignore it.</summary>
    public string? StickinessKey { get; }

    /// <summary>Claims. A rule matches when any pair has the required type and value.</summary>
    public IReadOnlyList<FeatureFlagClaim> Claims { get; }

    /// <summary>Attributes. A rule matches when any pair has the required key and value.</summary>
    public IReadOnlyList<FeatureFlagAttribute> Attributes { get; }

    internal FeatureFlagContext(string? userId, string? tenantId, string? stickinessKey, IReadOnlyList<FeatureFlagClaim> claims, IReadOnlyList<FeatureFlagAttribute> attributes)
    {
        if (userId is not null)
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(userId);

        if (tenantId is not null)
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(tenantId);

        if (stickinessKey is not null)
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(stickinessKey);

        UserId = userId;
        TenantId = tenantId;
        StickinessKey = stickinessKey;
        Claims = claims;
        Attributes = attributes;
    }

    /// <summary>Creates an empty context. Chain <c>With*</c> and call <see cref="FeatureFlagContextBuilder.Build" />.</summary>
    public static FeatureFlagContextBuilder Create() => new();
}

/// <summary>Builds a <see cref="FeatureFlagContext" />. Repeated claim and attribute calls append.</summary>
public sealed class FeatureFlagContextBuilder
{
    private readonly List<FeatureFlagAttribute> _attributes = [];
    private readonly List<FeatureFlagClaim> _claims = [];
    private string? _stickinessKey;
    private string? _tenantId;
    private string? _userId;

    internal FeatureFlagContextBuilder() { }

    /// <summary>Sets the user id. Whitespace throws.</summary>
    public FeatureFlagContextBuilder WithUser(string userId)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(userId);
        _userId = userId;
        return this;
    }

    /// <summary>Sets the tenant id. Whitespace throws.</summary>
    public FeatureFlagContextBuilder WithTenant(string tenantId)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(tenantId);
        _tenantId = tenantId;
        return this;
    }

    /// <summary>Appends a claim. Whitespace type or value throws.</summary>
    public FeatureFlagContextBuilder WithClaim(string type, string value)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(type);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(value);
        _claims.Add(new() { Type = type, Value = value });
        return this;
    }

    /// <summary>Appends an attribute. Whitespace key or value throws.</summary>
    public FeatureFlagContextBuilder WithAttribute(string key, string value)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(value);
        _attributes.Add(new() { Key = key, Value = value });
        return this;
    }

    /// <summary>Sets the percentage override key. Whitespace throws.</summary>
    public FeatureFlagContextBuilder WithStickiness(string stickinessKey)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(stickinessKey);
        _stickinessKey = stickinessKey;
        return this;
    }

    /// <summary>Returns the context. Further builder calls do not change it.</summary>
    public FeatureFlagContext Build() => new(_userId, _tenantId, _stickinessKey, _claims.ToArray(), _attributes.ToArray());
}