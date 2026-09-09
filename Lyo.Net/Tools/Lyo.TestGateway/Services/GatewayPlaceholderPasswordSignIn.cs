using Lyo.Authentication.Web.Components.Abstractions;
using Lyo.Authentication.Web.Components.Models;

namespace Lyo.TestGateway.Services;

/// <summary>
/// Stand-in <see cref="IAuthPasswordSignIn" /> for the Test Gateway. Registered so the username/password card appears on the shared login page, but Lyo has no password grant
/// yet — each attempt is refused with a short explanation. Replace with a real implementation (your <c>/account/login</c> endpoint, an identity provider password grant,
/// or similar) when local password sign-in is needed.
/// </summary>
internal sealed class GatewayPlaceholderPasswordSignIn : IAuthPasswordSignIn
{
    public Task<AuthPasswordSignInResult> SignInAsync(string username, string password, bool rememberMe, string? returnUrl, CancellationToken ct = default)
        => Task.FromResult(AuthPasswordSignInResult.Failure("Password sign-in is not configured for this gateway. Use one of the federated providers above."));
}