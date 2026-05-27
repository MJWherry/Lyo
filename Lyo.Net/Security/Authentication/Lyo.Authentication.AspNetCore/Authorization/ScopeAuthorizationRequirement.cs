using Microsoft.AspNetCore.Authorization;

namespace Lyo.Authentication.AspNetCore.Authorization;

/// <summary>Requires the caller's principal to hold a specific scope claim. Created on demand by <see cref="ScopeAuthorizationPolicyProvider"/>.</summary>
public sealed class ScopeAuthorizationRequirement : IAuthorizationRequirement
{
    /// <summary>The scope name (e.g. <c>people.read</c>).</summary>
    public string Scope { get; }

    /// <summary>Creates a new requirement.</summary>
    public ScopeAuthorizationRequirement(string scope) => Scope = scope;
}
