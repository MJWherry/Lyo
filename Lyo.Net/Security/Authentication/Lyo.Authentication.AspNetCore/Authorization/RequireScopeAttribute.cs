using System;
using Microsoft.AspNetCore.Authorization;

namespace Lyo.Authentication.AspNetCore.Authorization;

/// <summary>Convenience attribute equivalent to <c>[Authorize(Policy = "scope:&lt;scope&gt;")]</c>.</summary>
/// <example><c>[RequireScope("people.read")]</c></example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequireScopeAttribute : AuthorizeAttribute
{
    /// <summary>Creates a new attribute requiring the given <paramref name="scope"/>.</summary>
    public RequireScopeAttribute(string scope) => Policy = ScopeAuthorizationPolicyProvider.PolicyPrefix + scope;
}
