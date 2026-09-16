using Microsoft.AspNetCore.Authorization;

namespace Lyo.Authentication.AspNetCore.Authorization;

/// <summary>Shorthand for <c>[Authorize(Roles = "&lt;role&gt;")]</c>. The framework role requirement is the handler. There is no Lyo role store.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequireRoleAttribute : AuthorizeAttribute
{
    /// <summary>Requires <paramref name="role" />.</summary>
    public RequireRoleAttribute(string role) => Roles = role;
}
