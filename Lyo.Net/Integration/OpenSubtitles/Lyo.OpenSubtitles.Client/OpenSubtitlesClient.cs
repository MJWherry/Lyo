using Lyo.Exceptions;
using Lyo.Http.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.OpenSubtitles.Client;

/// <summary>OpenSubtitles consumer API client. Search and guessit use the API key. Download and user info also send the login token and stay on the hostname returned by login.</summary>
public class OpenSubtitlesClient : LyoHttpClient
{
    /// <summary>Login.</summary>
    public AuthManager Auth { get; }

    /// <summary>Subtitle search and download.</summary>
    public SubtitleManager Subtitles { get; }

    /// <summary>Authenticated user info.</summary>
    public InfoManager Info { get; }

    /// <summary>Filename parsing.</summary>
    public UtilitiesManager Utilities { get; }

    /// <summary>Creates a client. <paramref name="httpClient" /> is supplied by <c>IHttpClientFactory</c> when the client is resolved from DI.</summary>
    public OpenSubtitlesClient(OpenSubtitlesClientOptions options, ILoggerFactory? loggerFactory = null, HttpClient? httpClient = null)
        : base(
            loggerFactory?.CreateLogger<OpenSubtitlesClient>() ?? NullLoggerFactory.Instance.CreateLogger<OpenSubtitlesClient>(), httpClient, OpenSubtitlesJson.Create(), options)
    {
        ArgumentHelpers.ThrowIfNull(options);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(options.ApiKey);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(options.ApplicationUserAgent);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(options.BaseUrl);
        HttpClient.DefaultRequestHeaders.TryAddWithoutValidation("Api-Key", options.ApiKey);
        HttpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", options.ApplicationUserAgent);
        HttpClient.DefaultRequestHeaders.Accept.Add(new("application/json"));
        Auth = new(this);
        Subtitles = new(this);
        Info = new(this);
        Utilities = new(this);
    }

    internal string Url(OpenSubtitlesSession session, string relativePath)
    {
        ArgumentHelpers.ThrowIfNull(session);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(relativePath);
        return $"https://{session.Host}/api/v1/{relativePath.TrimStart('/')}";
    }

    internal static Action<HttpRequestMessage> Bearer(OpenSubtitlesSession session)
    {
        ArgumentHelpers.ThrowIfNull(session);
        return request => request.Headers.Authorization = new("Bearer", session.Token);
    }
}