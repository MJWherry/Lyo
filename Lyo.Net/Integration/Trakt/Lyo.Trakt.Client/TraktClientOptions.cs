using System.Diagnostics;
using Lyo.Http.Client;

namespace Lyo.Trakt.Client;

/// <summary>
/// Settings for <see cref="TraktClient" />. Bind from section <c>TraktClient</c>. Catalog calls use <see cref="LyoHttpClientOptions.BaseUrl" />. OAuth calls use
/// <c>https://auth.trakt.tv</c>.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public class TraktClientOptions : LyoHttpClientOptions
{
    /// <summary>Configuration section name.</summary>
    public new const string SectionName = "TraktClient";

    /// <summary>Application client id, sent as <c>trakt-api-key</c>. Required.</summary>
    public string ClientId { get; set; } = "";

    /// <summary>Application secret. Required for device-token polling, code exchange, and refresh.</summary>
    public string? ClientSecret { get; set; }

    /// <summary>Redirect URI registered on the Trakt app. Required for the authorization-code exchange.</summary>
    public string? RedirectUri { get; set; }

    /// <summary>Creates options pointed at <c>https://api.trakt.tv/</c>. 429 responses are returned to the caller so device-token polling can see slow-down.</summary>
    public TraktClientOptions()
    {
        BaseUrl = "https://api.trakt.tv/";
        RateLimit.HonorRetryAfter = false;
    }

    /// <summary>Client id, whether the client secret is set, redirect URI, and shared HTTP options. The secret is omitted.</summary>
    public override string ToString() => $"ClientId={ClientId} ClientSecretSet={!string.IsNullOrWhiteSpace(ClientSecret)} RedirectUri={RedirectUri} {base.ToString()}";
}