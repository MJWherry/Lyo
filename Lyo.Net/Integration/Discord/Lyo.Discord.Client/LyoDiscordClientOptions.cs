using Lyo.Api.Client;

namespace Lyo.Discord.Client;

/// <summary>Options for <see cref="LyoDiscordClient" /> (Lyo API base URL and HTTP behavior).</summary>
public sealed class LyoDiscordClientOptions
{
    /// <summary>Configuration section name for binding (e.g. <c>appsettings.json</c>).</summary>
    public const string SectionName = "LyoDiscordClient";

    /// <summary>Base URL of the Lyo API (trailing slash optional).</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>When true, non-success HTTP status codes throw <see cref="Lyo.Api.Models.Error.ApiException" />.</summary>
    public bool EnsureStatusCode { get; set; } = true;

    /// <summary>Maps to <see cref="ApiClientOptions.EnsureStatusCode" />.</summary>
    public ApiClientOptions ToApiClientOptions() => new() { BaseUrl = Url, EnsureStatusCode = EnsureStatusCode };
}