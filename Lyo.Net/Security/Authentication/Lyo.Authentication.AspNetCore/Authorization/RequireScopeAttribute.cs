using Microsoft.AspNetCore.Authorization;

namespace Lyo.Authentication.AspNetCore.Authorization;

/// <summary>Shorthand for <c>[Authorize(Policy = "scope:&lt;scope&gt;")]</c>.</summary>
/// <example>
/// <c>[RequireScope("people.read")]</c>
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequireScopeAttribute : AuthorizeAttribute
{
    /// <summary>Builds an attribute that requires <paramref name="scope" />.</summary>
    public RequireScopeAttribute(string scope) => Policy = ScopeAuthorizationPolicyProvider.PolicyPrefix + scope;
}