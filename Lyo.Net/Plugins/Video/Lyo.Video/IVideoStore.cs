namespace Lyo.Video;

/// <summary>Persistence for the video catalog: movies, series, seasons, episodes, credits, and identifiers.</summary>
public interface IVideoStore
{
    /// <summary>Upserts a title and replaces credits, identifiers, the YouTube link, seasons, and episodes. A movie that includes seasons is rejected.</summary>
    Task SaveTitleAsync(VideoTitle title, CancellationToken ct = default);

    /// <summary>Loads a title. Seasons and episodes are ordered by number.</summary>
    Task<VideoTitle?> GetTitleByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Loads a title by its unique slug.</summary>
    Task<VideoTitle?> GetTitleBySlugAsync(string slug, CancellationToken ct = default);

    /// <summary>Loads a title by scheme and value. IMDb, TMDB, and TVDB values are matched case-insensitively.</summary>
    Task<VideoTitle?> GetTitleByIdentifierAsync(string scheme, string value, CancellationToken ct = default);

    /// <summary>Deletes a title and its seasons, episodes, credits, and identifiers.</summary>
    Task DeleteTitleAsync(Guid id, CancellationToken ct = default);

    /// <summary>Upserts a genre. A parent cycle is rejected.</summary>
    Task SaveGenreAsync(VideoGenre genre, CancellationToken ct = default);

    /// <summary>Loads a genre by id.</summary>
    Task<VideoGenre?> GetGenreByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Lists genres ordered by name.</summary>
    Task<IReadOnlyList<VideoGenre>> ListGenresAsync(CancellationToken ct = default);

    /// <summary>Deletes a genre. Fails when a title or child genre still references it.</summary>
    Task DeleteGenreAsync(Guid id, CancellationToken ct = default);

    /// <summary>Searches titles. <see cref="VideoTitleQuery.FilterIds" /> null means no id filter. An empty list returns no rows.</summary>
    Task<IReadOnlyList<VideoTitle>> SearchTitlesAsync(VideoTitleQuery query, CancellationToken ct = default);

    /// <summary>Upserts a collection.</summary>
    Task SaveCollectionAsync(VideoCollection collection, CancellationToken ct = default);

    /// <summary>Deletes a collection and clears <see cref="VideoTitle.CollectionId" />. Titles stay.</summary>
    Task DeleteCollectionAsync(Guid id, CancellationToken ct = default);

    /// <summary>Upserts a cut. A second primary cut for the same title is rejected.</summary>
    Task SaveCutAsync(VideoCut cut, CancellationToken ct = default);

    /// <summary>Searches movie and episode transcripts. Title reads omit the transcript column.</summary>
    Task<IReadOnlyList<VideoTranscriptHit>> SearchTranscriptsAsync(VideoTranscriptQuery query, CancellationToken ct = default);

    /// <summary>Loads a movie transcript. Throws when the title does not exist.</summary>
    Task<string?> GetTitleTranscriptAsync(Guid titleId, CancellationToken ct = default);

    /// <summary>Replaces a movie transcript and refreshes the full-text index.</summary>
    Task SaveTitleTranscriptAsync(Guid titleId, string? transcript, CancellationToken ct = default);

    /// <summary>Loads an episode transcript. Throws when the episode does not exist.</summary>
    Task<string?> GetEpisodeTranscriptAsync(Guid episodeId, CancellationToken ct = default);

    /// <summary>Replaces an episode transcript and refreshes the full-text index.</summary>
    Task SaveEpisodeTranscriptAsync(Guid episodeId, string? transcript, CancellationToken ct = default);
}
