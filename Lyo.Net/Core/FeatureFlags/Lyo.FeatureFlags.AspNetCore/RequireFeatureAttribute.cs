using Lyo.Exceptions;

namespace Lyo.FeatureFlags.AspNetCore;

/// <summary>
/// Marks an endpoint as available only when every named flag is on. A disabled flag is a 404 with an empty body. Put it on an MVC controller or action. Minimal APIs use a
/// method-group attribute or <c>RequireFeature</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequireFeatureAttribute : Attribute
{
    /// <summary>Flag name passed to <c>IFeatureFlagService.IsEnabled</c>.</summary>
    public string Name { get; }

    /// <summary>Requires <paramref name="name" />. Whitespace throws. Several attributes on one endpoint must all be enabled.</summary>
    public RequireFeatureAttribute(string name)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }
}