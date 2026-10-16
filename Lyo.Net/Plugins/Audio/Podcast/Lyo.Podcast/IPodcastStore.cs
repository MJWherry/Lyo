namespace Lyo.Podcast;

/// <summary>Persistence for the podcast catalog: shows, seasons, episodes, credits, and identifiers.</summary>
public interface IPodcastStore
{
    /// <summary>Upserts a show and replaces credits, identifiers, seasons, and episodes. A null episode transcript leaves the stored text in place.</summary>
    Task SaveShowAsync(PodcastShow show, CancellationToken ct = default);

    /// <summary>Loads a show. Seasons and episodes are ordered by number. Transcripts are omitted.</summary>
    Task<PodcastShow?> GetShowByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Loads a show by its unique slug.</summary>
    Task<PodcastShow?> GetShowBySlugAsync(string slug, CancellationToken ct = default);

    /// <summary>Loads a show by scheme and value. iTunes and podcast-guid values are matched case-insensitively.</summary>
    Task<PodcastShow?> GetShowByIdentifierAsync(string scheme, string value, CancellationToken ct = default);

    /// <summary>Deletes a show and its seasons, episodes, credits, and identifiers.</summary>
    Task DeleteShowAsync(Guid id, CancellationToken ct = default);

    /// <summary>Searches shows. <see cref="PodcastShowQuery.FilterIds" /> null means no id filter. An empty list returns no rows.</summary>
    Task<IReadOnlyList<PodcastShow>> SearchShowsAsync(PodcastShowQuery query, CancellationToken ct = default);

    /// <summary>Upserts a genre. A parent cycle is rejected.</summary>
    Task SaveGenreAsync(PodcastGenre genre, CancellationToken ct = default);

    /// <summary>Loads a genre by id.</summary>
    Task<PodcastGenre?> GetGenreByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Lists genres ordered by name.</summary>
    Task<IReadOnlyList<PodcastGenre>> ListGenresAsync(CancellationToken ct = default);

    /// <summary>Deletes a genre. Fails when a show or child genre still references it.</summary>
    Task DeleteGenreAsync(Guid id, CancellationToken ct = default);

    /// <summary>Searches episode transcripts. Show reads omit the transcript column.</summary>
    Task<IReadOnlyList<PodcastTranscriptHit>> SearchTranscriptsAsync(PodcastTranscriptQuery query, CancellationToken ct = default);

    /// <summary>Loads an episode transcript. Throws when the episode does not exist.</summary>
    Task<string?> GetEpisodeTranscriptAsync(Guid episodeId, CancellationToken ct = default);

    /// <summary>Replaces an episode transcript. A null value clears it and refreshes the full-text index.</summary>
    Task SaveEpisodeTranscriptAsync(Guid episodeId, string? transcript, CancellationToken ct = default);
}
