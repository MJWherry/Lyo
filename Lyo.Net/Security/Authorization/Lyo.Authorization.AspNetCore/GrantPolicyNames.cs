using Lyo.Exceptions;

namespace Lyo.Authorization.AspNetCore;

/// <summary>Builds and parses <c>grant|action|type|resourceRoute|tenantRoute</c> policy names.</summary>
public static class GrantPolicyNames
{
    /// <summary>Prefix for grant policies.</summary>
    public const string Prefix = "grant|";

    /// <summary>Builds a policy name. Empty route names are omitted from the end. <c>|</c> is rejected in every segment.</summary>
    public static string For(string action, string resourceType, string? resourceIdRoute = null, string? tenantRoute = null)
    {
        var actionName = Segment(action, nameof(action));
        var typeName = Segment(resourceType, nameof(resourceType));
        var resource = string.IsNullOrWhiteSpace(resourceIdRoute) ? null : Segment(resourceIdRoute, nameof(resourceIdRoute));
        var tenant = string.IsNullOrWhiteSpace(tenantRoute) ? null : Segment(tenantRoute, nameof(tenantRoute));
        if (resource is null && tenant is null)
            return Prefix + actionName + "|" + typeName;

        if (tenant is null)
            return Prefix + actionName + "|" + typeName + "|" + resource;

        return Prefix + actionName + "|" + typeName + "|" + (resource ?? string.Empty) + "|" + tenant;
    }

    /// <summary>Reads a name produced by <see cref="For" />.</summary>
    public static bool TryParse(string policyName, out string action, out string resourceType, out string? resourceIdRoute, out string? tenantRoute)
    {
        action = string.Empty;
        resourceType = string.Empty;
        resourceIdRoute = null;
        tenantRoute = null;
        if (string.IsNullOrEmpty(policyName) || !policyName.StartsWith(Prefix, StringComparison.Ordinal))
            return false;

        var parts = policyName.Split('|');
        if (parts.Length < 3 || parts[1].Length == 0 || parts[2].Length == 0)
            return false;

        action = parts[1];
        resourceType = parts[2];
        if (parts.Length >= 4 && parts[3].Length > 0)
            resourceIdRoute = parts[3];

        if (parts.Length >= 5 && parts[4].Length > 0)
            tenantRoute = parts[4];

        return true;
    }

    private static string Segment(string value, string name)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(value);
        var trimmed = value.Trim();
        if (trimmed.Contains('|'))
            throw new ArgumentException(name + " cannot contain '|'.", name);

        return trimmed;
    }
}