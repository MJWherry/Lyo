namespace Lyo.Trakt.Client;

/// <summary>OAuth token payload. Store <see cref="RefreshToken" />; Trakt refresh tokens are single-use.</summary>
public sealed class TraktToken
{
    /// <summary>Bearer token for user endpoints. Valid for about 7 days.</summary>
    public string? AccessToken { get; set; }

    /// <summary>Refresh token. The next refresh must send this value, not the previous one.</summary>
    public string? RefreshToken { get; set; }

    /// <summary>Access-token lifetime in seconds.</summary>
    public int ExpiresIn { get; set; }

    /// <summary>Unix time when Trakt created the token.</summary>
    public int CreatedAt { get; set; }
}

/// <summary>Outcome of one <c>POST /oauth/device/token</c> poll.</summary>
public enum TraktDevicePollStatus
{
    /// <summary>The user approved the device code. <see cref="TraktDevicePoll.Token" /> is set.</summary>
    Authorized,

    /// <summary>HTTP 400. The user has not approved the code yet.</summary>
    Pending,

    /// <summary>HTTP 404. The device code is not valid.</summary>
    InvalidCode,

    /// <summary>HTTP 409. This device code was already exchanged.</summary>
    AlreadyUsed,

    /// <summary>HTTP 410. The device code expired.</summary>
    Expired,

    /// <summary>HTTP 418. The user denied the code.</summary>
    Denied,

    /// <summary>HTTP 429. The poll interval was too short.</summary>
    SlowDown
}

/// <summary>Device-token poll result. Non-success statuses are returned here instead of thrown.</summary>
public sealed class TraktDevicePoll
{
    /// <summary>Poll outcome.</summary>
    public TraktDevicePollStatus Status { get; init; }

    /// <summary>Token payload when <see cref="Status" /> is <see cref="TraktDevicePollStatus.Authorized" />.</summary>
    public TraktToken? Token { get; init; }
}
