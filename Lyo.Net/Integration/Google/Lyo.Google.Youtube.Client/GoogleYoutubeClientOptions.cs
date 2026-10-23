using System.Diagnostics;
using Lyo.Exceptions;
using Lyo.Http.Client;

namespace Lyo.Google.Youtube.Client;

/// <summary>Settings for the YouTube Data API client.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class GoogleYoutubeClientOptions : LyoHttpClientOptions
{
    /// <summary>Configuration section name.</summary>
    public new const string SectionName = "GoogleYoutube";

    /// <summary>Data API key sent as the <c>key</c> query parameter.</summary>
    public string ApiKey { get; set; } = string.Empty;

    public GoogleYoutubeClientOptions() => BaseUrl = "https://www.googleapis.com/youtube/v3/";

    /// <inheritdoc />
    public override void Validate()
    {
        base.Validate();
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(ApiKey);
    }

    /// <summary>Whether an API key is set, plus the shared HTTP options. The key value is omitted.</summary>
    public override string ToString() => $"ApiKeySet={!string.IsNullOrWhiteSpace(ApiKey)} {base.ToString()}";
}