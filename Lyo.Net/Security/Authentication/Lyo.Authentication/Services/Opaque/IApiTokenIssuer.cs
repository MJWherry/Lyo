using System.Threading;
using System.Threading.Tasks;
using Lyo.Authentication.Models.Records;

namespace Lyo.Authentication.Services.Opaque;

/// <summary>Mints new Format-B opaque API tokens, persists their hashed form, and returns the plaintext once.</summary>
public interface IApiTokenIssuer
{
    /// <summary>Issues a new token according to <paramref name="request"/>.</summary>
    /// <param name="request">Issuance parameters (kind, scopes, owner, optional metadata, optional lifetime override).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The plaintext wire-form token (shown to the caller exactly once) and the persisted record.</returns>
    Task<IssuedApiToken> IssueAsync(ApiTokenIssueRequest request, CancellationToken ct = default);
}
