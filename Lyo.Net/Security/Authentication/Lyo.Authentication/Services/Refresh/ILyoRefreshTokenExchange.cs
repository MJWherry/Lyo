using System.Threading;
using System.Threading.Tasks;
using Lyo.Authentication.Records;

namespace Lyo.Authentication.Services.Refresh;

/// <summary>
/// Exchanges a presented refresh token for a fresh access JWT (and a fresh refresh token — rotating refresh by default). Reuse of an already-revoked refresh token triggers an audit
/// event and invalidates the entire chain.
/// </summary>
public interface ILyoRefreshTokenExchange
{
    /// <summary>Exchanges <paramref name="presentedRefreshToken"/> for a new <see cref="IssuedLyoJwt"/>. Returns <c>null</c> on any failure (revoked, expired, malformed, theft).</summary>
    Task<IssuedLyoJwt?> ExchangeAsync(string presentedRefreshToken, CancellationToken ct = default);
}
