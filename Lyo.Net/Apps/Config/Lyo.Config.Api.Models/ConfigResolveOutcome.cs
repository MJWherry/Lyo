namespace Lyo.Config.Api.Models;

/// <summary>Result class for a conditional Config API resolve.</summary>
public enum ConfigResolveOutcome
{
    /// <summary>New payload, usually HTTP 200, carrying merged config when the caller asked for a body.</summary>
    Ok,

    /// <summary>Nothing changed; server answered <c>304 Not Modified</c>.</summary>
    NotModified,

    /// <summary>Failure path: non-success status, used when <see cref="ConfigApiClientOptions.EnsureStatusCode" /> is off.</summary>
    Failed
}