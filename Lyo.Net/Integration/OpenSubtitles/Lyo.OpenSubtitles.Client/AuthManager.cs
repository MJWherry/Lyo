using Lyo.Exceptions;

namespace Lyo.OpenSubtitles.Client;

/// <summary>Login against the consumer API.</summary>
public sealed class AuthManager(OpenSubtitlesClient client)
{
    /// <summary>Exchanges a username and password for a token and the hostname later calls must use.</summary>
    public async Task<OpenSubtitlesSession> LoginAsync(string username, string password, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(username);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(password);
        var response = await client.PostAsAsync<LoginRequest, LoginResponse>("login", new() { Username = username, Password = password }, ct: ct).ConfigureAwait(false);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(response.Token);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(response.BaseUrl);
        return new(response.Token, response.BaseUrl);
    }

    internal sealed class LoginRequest
    {
        public string? Username { get; set; }

        public string? Password { get; set; }
    }

    internal sealed class LoginResponse
    {
        public string? Token { get; set; }

        public string? BaseUrl { get; set; }
    }
}