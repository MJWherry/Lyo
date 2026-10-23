using Lyo.Authentication.Models.Records;

namespace Lyo.Authentication.Sso.Completion;

/// <summary>Links an <see cref="ExternalLoginAssertion" /> to a Lyo user and mints one access token plus a rotating refresh token.</summary>
public interface IExternalLoginCompletion
{
    /// <summary>Provisions or refreshes the user for <paramref name="assertion" /> and returns the minted token.</summary>
    /// <exception cref="ExternalLoginRejectedException">When policy, email verification, or a disabled user rejects the login.</exception>
    Task<IssuedLyoJwt> CompleteAsync(ExternalLoginAssertion assertion, CancellationToken ct = default);
}