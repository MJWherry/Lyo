using Lyo.Common.Core.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Lyo.Authentication.AspNetCore.Authorization;

/// <summary>
/// Builds <c>scope:&lt;name&gt;</c> policies on demand. Everything else goes to the default policy provider. Endpoints can opt into scope checks without
/// pre-registering every scope:
/// <code>
/// .RequireAuthorization("scope:people.read");
/// </code>
/// </summary>
public sealed class ScopeAuthorizationPolicyProvider : DefaultAuthorizationPolicyProvider
{
    /// <summary>Prefix that triggers on-demand scope-policy creation. Default = <c>scope:</c>.</summary>
    public const string PolicyPrefix = "scope:";

    /// <summary>Builds a new provider.</summary>
    public ScopeAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options)
        : base(options) { }

    /// <inheritdoc />
    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!policyName.IsNullOrEmpty() && policyName.StartsWith(PolicyPrefix, StringComparison.Ordinal)) {
            var scope = policyName.Substring(PolicyPrefix.Length);
            if (scope.Length == 0)
                return null;

            var builder = new AuthorizationPolicyBuilder();
            builder.RequireAuthenticatedUser();
            builder.AddRequirements(new ScopeAuthorizationRequirement(scope));
            return builder.Build();
        }

        return await base.GetPolicyAsync(policyName).ConfigureAwait(false);
    }
}