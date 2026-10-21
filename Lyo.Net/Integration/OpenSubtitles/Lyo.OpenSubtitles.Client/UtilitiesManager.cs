using Lyo.Exceptions;

namespace Lyo.OpenSubtitles.Client;

/// <summary>Filename helpers on the consumer API.</summary>
public sealed class UtilitiesManager(OpenSubtitlesClient client)
{
    /// <summary>Parses a media filename with <c>GET /utilities/guessit</c>.</summary>
    public Task<FilenameGuess?> GuessAsync(string filename, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(filename);
        return client.GetAsAsync<GuessQuery, FilenameGuess>("utilities/guessit", new() { Filename = filename }, ct: ct);
    }

    internal sealed class GuessQuery
    {
        public string? Filename { get; set; }
    }
}