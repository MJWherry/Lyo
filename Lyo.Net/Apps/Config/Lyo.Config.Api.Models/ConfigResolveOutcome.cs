namespace Lyo.Config.Api.Models;

/// <summary>Categorizes the result of a conditional resolve against the Config API.</summary>
public enum ConfigResolveOutcome
{
    /// <summary>Fresh response (typically 200) with merged config when requested.</summary>
    Ok,

    /// <summary>Resource unchanged (<c>304 Not Modified</c>).</summary>
    NotModified,

    /// <summary>Error response branch (non-success when <see cref="ConfigApiClientOptions.EnsureStatusCode" /> is false).</summary>
    Failed
}