namespace Lyo.Audiobook;

/// <summary>Persistence for the audiobook catalog: works, series, chapters, credits, and identifiers.</summary>
public interface IAudiobookStore
{
    /// <summary>Upserts a work and replaces credits, identifiers, and chapters. A null transcript leaves the stored text in place.</summary>
    Task SaveWorkAsync(Audiobook work, CancellationToken ct = default);

    /// <summary>Loads a work. Chapters are ordered by number. Transcripts are omitted.</summary>
    Task<Audiobook?> GetWorkByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Loads a work by its unique slug.</summary>
    Task<Audiobook?> GetWorkBySlugAsync(string slug, CancellationToken ct = default);

    /// <summary>Loads a work by scheme and value. ISBN values match on digits and a trailing X.</summary>
    Task<Audiobook?> GetWorkByIdentifierAsync(string scheme, string value, CancellationToken ct = default);

    /// <summary>Deletes a work and its chapters, credits, and identifiers.</summary>
    Task DeleteWorkAsync(Guid id, CancellationToken ct = default);

    /// <summary>Searches works. <see cref="AudiobookQuery.FilterIds" /> null means no id filter. An empty list returns no rows.</summary>
    Task<IReadOnlyList<Audiobook>> SearchWorksAsync(AudiobookQuery query, CancellationToken ct = default);

    /// <summary>Upserts a series.</summary>
    Task SaveSeriesAsync(AudiobookSeries series, CancellationToken ct = default);

    /// <summary>Loads a series by id.</summary>
    Task<AudiobookSeries?> GetSeriesByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Deletes a series and clears <see cref="Audiobook.SeriesId" /> and <see cref="Audiobook.SeriesIndex" />. Works stay.</summary>
    Task DeleteSeriesAsync(Guid id, CancellationToken ct = default);

    /// <summary>Upserts a genre. A parent cycle is rejected.</summary>
    Task SaveGenreAsync(AudiobookGenre genre, CancellationToken ct = default);

    /// <summary>Loads a genre by id.</summary>
    Task<AudiobookGenre?> GetGenreByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Lists genres ordered by name.</summary>
    Task<IReadOnlyList<AudiobookGenre>> ListGenresAsync(CancellationToken ct = default);

    /// <summary>Deletes a genre. Fails when a work or child genre still references it.</summary>
    Task DeleteGenreAsync(Guid id, CancellationToken ct = default);

    /// <summary>Searches work and chapter transcripts. Work reads omit the transcript column.</summary>
    Task<IReadOnlyList<AudiobookTranscriptHit>> SearchTranscriptsAsync(AudiobookTranscriptQuery query, CancellationToken ct = default);

    /// <summary>Loads a work transcript. Throws when the work does not exist.</summary>
    Task<string?> GetWorkTranscriptAsync(Guid workId, CancellationToken ct = default);

    /// <summary>Replaces a work transcript. A null value clears it and refreshes the full-text index.</summary>
    Task SaveWorkTranscriptAsync(Guid workId, string? transcript, CancellationToken ct = default);

    /// <summary>Loads a chapter transcript. Throws when the chapter does not exist.</summary>
    Task<string?> GetChapterTranscriptAsync(Guid chapterId, CancellationToken ct = default);

    /// <summary>Replaces a chapter transcript. A null value clears it and refreshes the full-text index.</summary>
    Task SaveChapterTranscriptAsync(Guid chapterId, string? transcript, CancellationToken ct = default);
}
