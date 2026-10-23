using System.Diagnostics;
using Lyo.Http.Client;

namespace Lyo.OpenSubtitles.Client;

/// <summary>
/// Settings for <see cref="OpenSubtitlesClient" />. Bind from section <c>OpenSubtitlesClient</c>. <see cref="LyoHttpClientOptions.BaseUrl" /> defaults to the consumer API
/// root.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public class OpenSubtitlesClientOptions : LyoHttpClientOptions
{
    /// <summary>Configuration section name.</summary>
    public new const string SectionName = "OpenSubtitlesClient";

    /// <summary>Consumer API key sent as <c>Api-Key</c>. Required.</summary>
    public string ApiKey { get; set; } = "";

    /// <summary>Application identity sent as <c>User-Agent</c>, in the form <c>AppName vVersion</c>. Required. Leave the inherited Chrome pool disabled.</summary>
    public string ApplicationUserAgent { get; set; } = "";

    /// <summary>Creates options pointed at <c>https://api.opensubtitles.com/api/v1/</c>.</summary>
    public OpenSubtitlesClientOptions() => BaseUrl = "https://api.opensubtitles.com/api/v1/";

    /// <summary>Whether an API key is set, plus the shared HTTP options. The key value is omitted.</summary>
    public override string ToString() => $"ApiKeySet={!string.IsNullOrWhiteSpace(ApiKey)} ApplicationUserAgent={ApplicationUserAgent} {base.ToString()}";
}