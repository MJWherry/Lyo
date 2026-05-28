using Lyo.Authentication.Models.Records;
using Lyo.Authentication.Scopes;
using Microsoft.AspNetCore.Authorization;

namespace Lyo.Authentication.AspNetCore.Authorization;

/// <summary>
/// Handles <see cref="ScopeAuthorizationRequirement" />. Walks every <c>scope</c> claim the principal holds (a) as one-per-claim entries (opaque-token shape) and (b) as a
/// single space-delimited entry (legacy JWT shape; the Lyo JWT validator already splits on issuance, but we still accept the legacy form for cross-issuer compatibility). When an
/// <see cref="IScopeRegistry" /> is registered, each held scope is expanded under <see cref="Scope.TransitiveImplies" /> so a holder of <c>admin</c> implicitly satisfies
/// <c>people.read</c> when that relationship has been declared.
/// </summary>
public sealed class ScopeAuthorizationHandler : AuthorizationHandler<ScopeAuthorizationRequirement>
{
    private readonly IScopeRegistry _registry;

    /// <summary>Creates a new handler. <paramref name="registry" /> drives transitive scope expansion at authorization time.</summary>
    public ScopeAuthorizationHandler(IScopeRegistry registry) => _registry = registry;

    /// <inheritdoc />
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ScopeAuthorizationRequirement requirement)
    {
        if (context.User?.Identity?.IsAuthenticated != true)
            return Task.CompletedTask;

        foreach (var claim in context.User.FindAll(LyoJwtClaims.Scope)) {
            var value = claim.Value;
            if (value.IndexOf(' ') < 0) {
                if (Matches(value, requirement.Scope)) {
                    context.Succeed(requirement);
                    return Task.CompletedTask;
                }

                continue;
            }

            foreach (var s in value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)) {
                if (Matches(s, requirement.Scope)) {
                    context.Succeed(requirement);
                    return Task.CompletedTask;
                }
            }
        }

        return Task.CompletedTask;
    }

    private bool Matches(string held, string required)
    {
        if (string.Equals(held, required, StringComparison.Ordinal))
            return true;

        var heldScope = _registry.TryGet(held);
        if (heldScope is null)
            return false;

        foreach (var implied in heldScope.TransitiveImplies) {
            if (string.Equals(implied, required, StringComparison.Ordinal))
                return true;
        }

        return false;
    }
}