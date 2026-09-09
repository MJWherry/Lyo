namespace Lyo.Comic;

/// <summary>Comic series, volumes, and chapters persistence abstraction.</summary>
public interface IComicStore
{
    /// <summary>Upserts a comic series, including its alternate titles.</summary>
    Task SaveSeriesAsync(ComicSeries series, CancellationToken ct = default);

    /// <summary>Looks up a series by id, including its alternate titles. Returns null if not found.</summary>
    Task<ComicSeries?> GetSeriesByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Looks up a series by slug, including its alternate titles. Returns null if not found.</summary>
    Task<ComicSeries?> GetSeriesBySlugAsync(string slug, CancellationToken ct = default);

    /// <summary>Series matching the given query. Results include alternate titles searches.</summary>
    Task<IReadOnlyList<ComicSeries>> SearchSeriesAsync(ComicSeriesQuery query, CancellationToken ct = default);

    /// <summary>Removes a series and all of its alternate titles, volumes, and chapters.</summary>
    Task DeleteSeriesAsync(Guid id, CancellationToken ct = default);

    /// <summary>Upserts a volume.</summary>
    Task SaveVolumeAsync(ComicVolume volume, CancellationToken ct = default);

    /// <summary>Loads a volume by id. Returns null if not found.</summary>
    Task<ComicVolume?> GetVolumeByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Loads all volumes for a series, ordered by volume number ascending.</summary>
    Task<IReadOnlyList<ComicVolume>> GetVolumesBySeriesAsync(Guid seriesId, CancellationToken ct = default);

    /// <summary>Removes a volume and all of its chapters.</summary>
    Task DeleteVolumeAsync(Guid id, CancellationToken ct = default);

    /// <summary>Upserts a chapter.</summary>
    Task SaveChapterAsync(ComicChapter chapter, CancellationToken ct = default);

    /// <summary>Returns a chapter by id. Returns null if not found.</summary>
    Task<ComicChapter?> GetChapterByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Loads all chapters for a series, optionally filtered by language. Results are ordered by chapter number ascending, then by language.</summary>
    Task<IReadOnlyList<ComicChapter>> GetChaptersBySeriesAsync(Guid seriesId, string? language = null, CancellationToken ct = default);

    /// <summary>Lists all chapters belonging to a volume, optionally filtered by language. Results are ordered by chapter number ascending.</summary>
    Task<IReadOnlyList<ComicChapter>> GetChaptersByVolumeAsync(Guid volumeId, string? language = null, CancellationToken ct = default);

    /// <summary>Removes a chapter by id.</summary>
    Task DeleteChapterAsync(Guid id, CancellationToken ct = default);

    /// <summary>Upserts a page.</summary>
    Task SavePageAsync(ComicPage page, CancellationToken ct = default);

    /// <summary>Loads a page by id. Returns null if not found.</summary>
    Task<ComicPage?> GetPageByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Loads all pages for a chapter, ordered by page number ascending.</summary>
    Task<IReadOnlyList<ComicPage>> GetPagesByChapterAsync(Guid chapterId, CancellationToken ct = default);

    /// <summary>Removes a page by id.</summary>
    Task DeletePageAsync(Guid id, CancellationToken ct = default);

    /// <summary>Upserts a character.</summary>
    Task SaveCharacterAsync(ComicCharacter character, CancellationToken ct = default);

    /// <summary>Loads a character by id. Returns null if not found.</summary>
    Task<ComicCharacter?> GetCharacterByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Loads all characters for a series, ordered by name ascending.</summary>
    Task<IReadOnlyList<ComicCharacter>> GetCharactersBySeriesAsync(Guid seriesId, CancellationToken ct = default);

    /// <summary>Removes a character by id.</summary>
    Task DeleteCharacterAsync(Guid id, CancellationToken ct = default);
}