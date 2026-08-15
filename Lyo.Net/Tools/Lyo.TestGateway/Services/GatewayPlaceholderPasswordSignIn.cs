using Lyo.Authentication.Web.Components.Abstractions;
using Lyo.Authentication.Web.Components.Models;

namespace Lyo.TestGateway.Services;

/// <summary>
/// Placeholder <see cref="IAuthPasswordSignIn" /> for the Test Gateway. Registered so the username/password card renders on the shared login page, but no Lyo password grant
/// exists today — every attempt is rejected with a friendly explanation. Swap for a real impl (against your own <c>/account/login</c> endpoint, an identity provider's password grant,
/// etc.) when local password auth is wanted.
/// </summary>
internal sealed class GatewayPlaceholderPasswordSignIn : IAuthPasswordSignIn
{
    public Task<AuthPasswordSignInResult> SignInAsync(string username, string password, bool rememberMe, string? returnUrl, CancellationToken ct = default)
        => Task.FromResult(AuthPasswordSignInResult.Failure("Password sign-in is not configured for this gateway. Use one of the federated providers above."));
}