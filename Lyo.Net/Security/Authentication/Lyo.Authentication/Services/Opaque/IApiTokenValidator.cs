using System.Threading;
using System.Threading.Tasks;
using Lyo.Authentication.Records;

namespace Lyo.Authentication.Services.Opaque;

/// <summary>Validates a presented Format-B token. Returns <c>null</c> for every kind of failure — never leaks why to the caller (audit captures detail).</summary>
public interface IApiTokenValidator
{
    /// <summary>Validates <paramref name="presentedToken"/>. Returns the principal on success, <c>null</c> on any failure.</summary>
    /// <param name="presentedToken">The raw bearer string off the wire.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<ApiTokenPrincipal?> ValidateAsync(string presentedToken, CancellationToken ct = default);
}
