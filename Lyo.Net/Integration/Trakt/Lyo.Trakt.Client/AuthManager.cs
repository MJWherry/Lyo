using Lyo.Exceptions;
using Lyo.Http.Client;

namespace Lyo.Trakt.Client;

/// <summary>OAuth calls against <c>https://auth.trakt.tv</c>.</summary>
public sealed class AuthManager(TraktClient client)
{
    private const string TokenUrl = "https://auth.trakt.tv/oauth/token";
    private const string DeviceTokenUrl = "https://auth.trakt.tv/oauth/device/token";

    /// <summary>
    /// Exchanges an authorization code for tokens. Requires <see cref="TraktClientOptions.ClientSecret" /> and <see cref="TraktClientOptions.RedirectUri" />. Keep the returned
    /// refresh token; the previous one cannot be reused.
    /// </summary>
    public Task<TraktToken> ExchangeCodeAsync(string code, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(code);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(client.Settings.ClientSecret);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(client.Settings.RedirectUri);
        return client.PostAsAsync<CodeExchangeRequest, TraktToken>(
            TokenUrl, new() {
                Code = code,
                ClientId = client.Settings.ClientId,
                ClientSecret = client.Settings.ClientSecret,
                RedirectUri = client.Settings.RedirectUri
            }, ct: ct);
    }

    /// <summary>Polls <c>POST /oauth/device/token</c>. Pending, expired, denied, and slow-down come back as <see cref="TraktDevicePoll.Status" /> instead of an exception.</summary>
    public async Task<TraktDevicePoll> PollDeviceTokenAsync(string deviceCode, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(deviceCode);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(client.Settings.ClientSecret);
        try {
            var token = await client.PostAsAsync<DeviceTokenRequest, TraktToken>(
                    DeviceTokenUrl, new() { Code = deviceCode, ClientId = client.Settings.ClientId, ClientSecret = client.Settings.ClientSecret }, ct: ct)
                .ConfigureAwait(false);

            return new() { Status = TraktDevicePollStatus.Authorized, Token = token };
        }
        catch (LyoHttpException ex) when (TryMapPollStatus(ex.StatusCode, out var status)) {
            return new() { Status = status };
        }
    }

    private static bool TryMapPollStatus(int statusCode, out TraktDevicePollStatus status)
    {
        switch (statusCode) {
            case 400:
                status = TraktDevicePollStatus.Pending;
                return true;
            case 404:
                status = TraktDevicePollStatus.InvalidCode;
                return true;
            case 409:
                status = TraktDevicePollStatus.AlreadyUsed;
                return true;
            case 410:
                status = TraktDevicePollStatus.Expired;
                return true;
            case 418:
                status = TraktDevicePollStatus.Denied;
                return true;
            case 429:
                status = TraktDevicePollStatus.SlowDown;
                return true;
            default:
                status = default;
                return false;
        }
    }

    internal sealed class CodeExchangeRequest
    {
        public string GrantType { get; set; } = "authorization_code";

        public string? Code { get; set; }

        public string? ClientId { get; set; }

        public string? ClientSecret { get; set; }

        public string? RedirectUri { get; set; }
    }

    internal sealed class DeviceTokenRequest
    {
        public string? Code { get; set; }

        public string? ClientId { get; set; }

        public string? ClientSecret { get; set; }
    }
}