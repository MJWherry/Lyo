using Lyo.Api.Models.Common.Response;
using Lyo.Api.Services.Crud.Read;
using Lyo.Api.Services.Crud.Read.Query;
using Lyo.Comic.Api.Models.Response;
using Lyo.Comment;
using Lyo.Comment.Postgres.Database;
using Lyo.Common.Enums;
using Lyo.EntityReference.Models;
using Lyo.Favorite;
using Lyo.Favorite.Postgres.Database;
using Lyo.Query.Models.Builders;
using Lyo.Rating;
using Lyo.Rating.Postgres.Database;
using Lyo.Tag;
using Lyo.Tag.Postgres.Database;
using Microsoft.Extensions.Options;

namespace Lyo.Comic.Api.Services;

/// <summary>
/// Aggregates cross-domain metadata for comic entities. List enrichment uses <see cref="IQueryService{TContext}" /> + <see cref="QueryReqBuilder" /> per bounded schema so
/// results participate in Lyo.Api query caching; favorite totals use store aggregation (GROUP BY) to avoid loading every favorite row.
/// </summary>
public sealed class ComicEnrichmentService
{
    /// <summary>Caps SQL IN lists; chunked queries are merged in-memory.</summary>
    private const int IdInClauseChunkSize = 400;

    private const string SeriesEntityType = "ComicSeries";
    private const string VolumeEntityType = "ComicVolume";
    private const string ChapterEntityType = "ComicChapter";
    private readonly IQueryService<CommentDbContext> _commentQueryService;
    private readonly ICommentStore _commentStore;
    private readonly IQueryService<FavoriteDbContext> _favoriteQueryService;
    private readonly IFavoriteStore _favoriteStore;
    private readonly ILogger<ComicEnrichmentService> _logger;
    private readonly int _maxQueryPageSize;
    private readonly IQueryService<RatingDbContext> _ratingQueryService;
    private readonly IRatingStore _ratingStore;
    private readonly IQueryService<TagDbContext> _tagQueryService;
    private readonly ITagStore _tagStore;

    public ComicEnrichmentService(
        ITagStore tagStore,
        IRatingStore ratingStore,
        ICommentStore commentStore,
        IFavoriteStore favoriteStore,
        IQueryService<TagDbContext> tagQueryService,
        IQueryService<RatingDbContext> ratingQueryService,
        IQueryService<CommentDbContext> commentQueryService,
        IQueryService<FavoriteDbContext> favoriteQueryService,
        IOptions<QueryOptions> queryOptions,
        ILogger<ComicEnrichmentService> logger)
    {
        _tagStore = tagStore;
        _ratingStore = ratingStore;
        _commentStore = commentStore;
        _favoriteStore = favoriteStore;
        _tagQueryService = tagQueryService;
        _ratingQueryService = ratingQueryService;
        _commentQueryService = commentQueryService;
        _favoriteQueryService = favoriteQueryService;
        _maxQueryPageSize = queryOptions.Value.MaxPageSize;
        _logger = logger;
    }

    /// <summary>Enriches a series domain model with cross-domain metadata.</summary>
    public async Task<ComicSeriesRes> EnrichSeriesAsync(ComicSeries series, EntityRef? callerRef = null, CancellationToken ct = default)
    {
        var entityRef = EntityRef.ForGuid(SeriesEntityType, series.Id);
        var tagsTask = _tagStore.GetTagsForEntityAsync(entityRef, ct: ct);
        var ratingsTask = _ratingStore.GetForEntityAsync(entityRef, ct);
        var commentsTask = _commentStore.GetForEntityAsync(entityRef, false, ct);
        var favoriteCountTask = _favoriteStore.GetCountForEntityAsync(entityRef, ct: ct);
        var isFavoritedTask = callerRef.HasValue
            ? _favoriteStore.IsFavoritedAsync(entityRef, callerRef.Value, ct: ct).ContinueWith(t => (bool?)t.Result, TaskContinuationOptions.ExecuteSynchronously)
            : Task.FromResult<bool?>(null);

        await Task.WhenAll(tagsTask, ratingsTask, commentsTask, favoriteCountTask, isFavoritedTask).ConfigureAwait(false);
        var tags = (await tagsTask).Select(t => t.Name).ToList();
        return ToSeriesRes(series, tags, await ratingsTask, (await commentsTask).Count, await favoriteCountTask, await isFavoritedTask);
    }

    /// <summary>Enriches a volume domain model with cross-domain metadata.</summary>
    public async Task<ComicVolumeRes> EnrichVolumeAsync(ComicVolume volume, EntityRef? callerRef = null, CancellationToken ct = default)
    {
        var entityRef = EntityRef.ForGuid(VolumeEntityType, volume.Id);
        var tagsTask = _tagStore.GetTagsForEntityAsync(entityRef, ct: ct);
        var ratingsTask = _ratingStore.GetForEntityAsync(entityRef, ct);
        var commentsTask = _commentStore.GetForEntityAsync(entityRef, false, ct);
        var favoriteCountTask = _favoriteStore.GetCountForEntityAsync(entityRef, ct: ct);
        var isFavoritedTask = callerRef.HasValue
            ? _favoriteStore.IsFavoritedAsync(entityRef, callerRef.Value, ct: ct).ContinueWith(t => (bool?)t.Result, TaskContinuationOptions.ExecuteSynchronously)
            : Task.FromResult<bool?>(null);

        await Task.WhenAll(tagsTask, ratingsTask, commentsTask, favoriteCountTask, isFavoritedTask).ConfigureAwait(false);
        var ratings = await ratingsTask;
        var ratingValues = ratings.Where(r => r.Value.HasValue).Select(r => r.Value!.Value).ToList();
        return new() {
            Id = volume.Id,
            SeriesId = volume.SeriesId,
            VolumeNumber = volume.VolumeNumber,
            Title = volume.Title,
            CoverImageRef = volume.CoverImageRef,
            PublishedDate = volume.PublishedDate,
            CreatedTimestamp = volume.CreatedTimestamp,
            UpdatedTimestamp = volume.UpdatedTimestamp,
            Tags = (await tagsTask).Select(t => t.Name).ToList(),
            AverageRating = ratingValues.Count > 0 ? Math.Round(ratingValues.Average(), 2) : null,
            RatingCount = ratings.Count,
            CommentCount = (await commentsTask).Count,
            FavoriteCount = await favoriteCountTask,
            IsFavorited = await isFavoritedTask
        };
    }

    /// <summary>Enriches a chapter domain model with cross-domain metadata.</summary>
    public async Task<ComicChapterRes> EnrichChapterAsync(ComicChapter chapter, EntityRef? callerRef = null, CancellationToken ct = default)
    {
        var entityRef = EntityRef.ForGuid(ChapterEntityType, chapter.Id);
        var tagsTask = _tagStore.GetTagsForEntityAsync(entityRef, ct: ct);
        var ratingsTask = _ratingStore.GetForEntityAsync(entityRef, ct);
        var commentsTask = _commentStore.GetForEntityAsync(entityRef, false, ct);
        var favoriteCountTask = _favoriteStore.GetCountForEntityAsync(entityRef, ct: ct);
        var isFavoritedTask = callerRef.HasValue
            ? _favoriteStore.IsFavoritedAsync(entityRef, callerRef.Value, ct: ct).ContinueWith(t => (bool?)t.Result, TaskContinuationOptions.ExecuteSynchronously)
            : Task.FromResult<bool?>(null);

        await Task.WhenAll(tagsTask, ratingsTask, commentsTask, favoriteCountTask, isFavoritedTask).ConfigureAwait(false);
        var ratings = await ratingsTask;
        var ratingValues = ratings.Where(r => r.Value.HasValue).Select(r => r.Value!.Value).ToList();
        return new() {
            Id = chapter.Id,
            SeriesId = chapter.SeriesId,
            VolumeId = chapter.VolumeId,
            ChapterNumber = chapter.ChapterNumber,
            Title = chapter.Title,
            Language = chapter.Language,
            PageCount = chapter.PageCount,
            PublishedDate = chapter.PublishedDate,
            Source = chapter.Source,
            CoverImageRef = chapter.CoverImageRef,
            CreatedTimestamp = chapter.CreatedTimestamp,
            UpdatedTimestamp = chapter.UpdatedTimestamp,
            Tags = (await tagsTask).Select(t => t.Name).ToList(),
            AverageRating = ratingValues.Count > 0 ? Math.Round(ratingValues.Average(), 2) : null,
            RatingCount = ratings.Count,
            CommentCount = (await commentsTask).Count,
            FavoriteCount = await favoriteCountTask,
            IsFavorited = await isFavoritedTask
        };
    }

    /// <summary>Enriches a list of series using batched <see cref="IQueryService{TContext}" /> reads per schema (cached) plus aggregated favorite counts.</summary>
    public async Task<ComicSeriesRes[]> EnrichSeriesListAsync(IReadOnlyList<ComicSeries> items, EntityRef? callerRef = null, CancellationToken ct = default)
    {
        if (items.Count == 0)
            return [];

        var seriesGuids = items.Select(s => s.Id).Distinct().ToArray();
        if (seriesGuids.Length == 0)
            return [];

        var tagsTask = QueryTagEntitiesForSeriesAsync(seriesGuids, ct);
        var ratingsTask = QueryRatingEntitiesForSeriesAsync(seriesGuids, ct);
        var commentsTask = QueryTopLevelCommentEntitiesForSeriesAsync(seriesGuids, ct);
        var favoriteCountsTask = _favoriteStore.GetFavoriteCountsForEntitiesAsync(SeriesEntityType, seriesGuids, ct: ct);
        var favoritedTask = callerRef.HasValue ? QueryFavoritedSeriesIdsAsync(callerRef.Value, seriesGuids, ct) : Task.FromResult(new HashSet<Guid>());
        await Task.WhenAll(tagsTask, ratingsTask, commentsTask, favoriteCountsTask, favoritedTask).ConfigureAwait(false);
        var tagsByEntityId = (await tagsTask).GroupBy(t => t.ForEntityId).ToDictionary(static g => g.Key, static g => g.Select(x => x.Name).OrderBy(n => n).ToList());
        var ratingsByEntityId = (await ratingsTask).GroupBy(r => r.ForEntityId).ToDictionary(static g => g.Key, static g => g.Select(ToRatingRecord).ToList());
        var commentCountsByEntityId = (await commentsTask).GroupBy(c => c.ForEntityId).ToDictionary(static g => g.Key, static g => g.Count());
        var favoriteCounts = await favoriteCountsTask;
        var favoritedLookup = await favoritedTask;
        var results = new ComicSeriesRes[items.Count];
        for (var i = 0; i < items.Count; i++) {
            var series = items[i];
            tagsByEntityId.TryGetValue(series.Id, out var tagNames);
            tagNames ??= [];
            ratingsByEntityId.TryGetValue(series.Id, out var ratings);
            ratings ??= [];
            var commentCount = commentCountsByEntityId.GetValueOrDefault(series.Id);
            var favoriteCount = favoriteCounts.TryGetValue(series.Id, out var fc) ? fc : 0;
            bool? isFavorited = callerRef.HasValue ? favoritedLookup.Contains(series.Id) : null;
            results[i] = ToSeriesRes(series, tagNames, ratings, commentCount, favoriteCount, isFavorited);
        }

        return results;
    }

    /// <summary>Enriches a list of volumes in parallel.</summary>
    public Task<ComicVolumeRes[]> EnrichVolumeListAsync(IReadOnlyList<ComicVolume> items, EntityRef? callerRef = null, CancellationToken ct = default)
        => Task.WhenAll(items.Select(v => EnrichVolumeAsync(v, callerRef, ct)));

    /// <summary>Enriches a list of chapters in parallel.</summary>
    public Task<ComicChapterRes[]> EnrichChapterListAsync(IReadOnlyList<ComicChapter> items, EntityRef? callerRef = null, CancellationToken ct = default)
        => Task.WhenAll(items.Select(c => EnrichChapterAsync(c, callerRef, ct)));

    private Task<List<TagEntity>> QueryTagEntitiesForSeriesAsync(Guid[] distinctIds, CancellationToken ct)
        => QueryChunksAsync(
            "tags", distinctIds, chunk => {
                var req = QueryReqBuilder.New()
                    .For<TagEntity>()
                    .AddWhere(w => {
                        w.AddEquals(t => t.ForEntityType, SeriesEntityType);
                        w.In(t => t.ForEntityId, chunk);
                    })
                    .AddSort(e => e.ForEntityId, SortDirection.Asc)
                    .AddSort(e => e.Name, SortDirection.Asc)
                    .Done()
                    .SetPagination(0, _maxQueryPageSize)
                    .Build();

                return _tagQueryService.Query<TagEntity>(req, e => e.ForEntityId, SortDirection.Asc, ct);
            }, ct);

    private Task<List<RatingEntity>> QueryRatingEntitiesForSeriesAsync(Guid[] distinctIds, CancellationToken ct)
        => QueryChunksAsync(
            "ratings", distinctIds, chunk => {
                var req = QueryReqBuilder.New()
                    .For<RatingEntity>()
                    .AddWhere(w => {
                        w.AddEquals(r => r.ForEntityType, SeriesEntityType);
                        w.In(r => r.ForEntityId, chunk);
                    })
                    .AddSort(e => e.ForEntityId, SortDirection.Asc)
                    .Done()
                    .SetPagination(0, _maxQueryPageSize)
                    .Build();

                return _ratingQueryService.Query<RatingEntity>(req, e => e.ForEntityId, SortDirection.Asc, ct);
            }, ct);

    private Task<List<CommentEntity>> QueryTopLevelCommentEntitiesForSeriesAsync(Guid[] distinctIds, CancellationToken ct)
        => QueryChunksAsync(
            "comments", distinctIds, chunk => {
                var req = QueryReqBuilder.New()
                    .For<CommentEntity>()
                    .AddWhere(w => {
                        w.AddEquals(c => c.ForEntityType, SeriesEntityType);
                        w.In(c => c.ForEntityId, chunk);
                        w.AddEquals(c => c.ReplyToCommentId, null);
                    })
                    .AddSort(e => e.ForEntityId, SortDirection.Asc)
                    .Done()
                    .SetPagination(0, _maxQueryPageSize)
                    .Build();

                return _commentQueryService.Query<CommentEntity>(req, e => e.ForEntityId, SortDirection.Asc, ct);
            }, ct);

    private async Task<HashSet<Guid>> QueryFavoritedSeriesIdsAsync(EntityRef caller, Guid[] distinctIds, CancellationToken ct)
    {
        var favorited = new HashSet<Guid>();
        var appliedId = EntityRefPersistedGuid.RequirePersistedGuid(caller);
        foreach (var chunk in distinctIds.Chunk(IdInClauseChunkSize)) {
            var chunkArr = chunk.ToArray();
            var req = QueryReqBuilder.New()
                .For<FavoriteEntity>()
                .AddWhere(w => {
                    w.AddEquals(f => f.ForEntityType, SeriesEntityType);
                    w.In(f => f.ForEntityId, chunkArr);
                    w.AddEquals(f => f.FromEntityType, caller.EntityType);
                    w.AddEquals(f => f.FromEntityId, appliedId);
                })
                .AddSort(e => e.ForEntityId, SortDirection.Asc)
                .Done()
                .SetPagination(0, _maxQueryPageSize)
                .Build();

            var res = await _favoriteQueryService.Query<FavoriteEntity>(req, e => e.ForEntityId, SortDirection.Asc, ct).ConfigureAwait(false);
            if (!TryDrainQuery(res, "favorites(caller)", out var rows))
                continue;

            foreach (var row in rows)
                favorited.Add(row.ForEntityId);
        }

        return favorited;
    }

    private async Task<List<TEntity>> QueryChunksAsync<TEntity>(
        string dimensionLabel,
        Guid[] distinctIds,
        Func<Guid[], Task<QueryRes<TEntity>>> queryChunk,
        CancellationToken ct)
        where TEntity : class
    {
        var combined = new List<TEntity>();
        foreach (var chunk in distinctIds.Chunk(IdInClauseChunkSize)) {
            ct.ThrowIfCancellationRequested();
            var chunkArr = chunk.ToArray();
            var res = await queryChunk(chunkArr).ConfigureAwait(false);
            if (!TryDrainQuery(res, dimensionLabel, out var rows))
                continue;

            combined.AddRange(rows);
        }

        return combined;
    }

    private bool TryDrainQuery<T>(QueryRes<T> res, string dimensionLabel, out IReadOnlyList<T> rows)
        where T : class
    {
        rows = res.Items ?? [];
        if (res.IsSuccess)
            return true;

        _logger.LogWarning("Comic batch {Dimension} QueryService call failed: {Detail}", dimensionLabel, res.Error?.Detail ?? res.Error?.Title ?? "unknown");
        return false;
    }

    private static RatingRecord ToRatingRecord(RatingEntity e)
        => new() {
            Id = e.Id,
            ForEntityType = e.ForEntityType,
            ForEntityId = e.ForEntityId,
            FromEntityType = e.FromEntityType,
            FromEntityId = e.FromEntityId,
            TenantId = e.TenantId,
            Context = e.Context,
            CreatedAt = e.CreatedAt,
            ExpiresAt = e.ExpiresAt,
            DeletedAt = e.DeletedAt,
            DeletedByType = e.DeletedByType,
            DeletedById = e.DeletedById,
            MetadataJson = e.MetadataJson,
            Visibility = e.Visibility,
            Subject = e.Subject,
            Title = e.Title,
            Value = e.Value,
            Message = e.Message,
            LikeCount = e.LikeCount,
            DislikeCount = e.DislikeCount,
            UpdatedTimestamp = e.UpdatedTimestamp
        };

    private static ComicSeriesRes ToSeriesRes(
        ComicSeries series,
        IReadOnlyList<string> tags,
        IReadOnlyList<RatingRecord> ratings,
        int commentCount,
        int favoriteCount,
        bool? isFavorited)
    {
        var ratingValues = ratings.Where(r => r.Value.HasValue).Select(r => r.Value!.Value).ToList();
        return new() {
            Id = series.Id,
            Title = series.Title,
            Slug = series.Slug,
            ComicType = series.ComicType,
            Status = series.Status,
            Description = series.Description,
            Language = series.Language,
            PublishedYear = series.PublishedYear,
            Author = series.Author,
            Artist = series.Artist,
            Publisher = series.Publisher,
            Source = series.Source,
            CoverImageRef = series.CoverImageRef,
            Demographic = series.Demographic,
            CreatedTimestamp = series.CreatedTimestamp,
            UpdatedTimestamp = series.UpdatedTimestamp,
            AlternateTitles = series.AlternateTitles.Select(a => new ComicAlternateTitleRes { Id = a.Id, Title = a.Title, Language = a.Language }).ToList(),
            Tags = tags.ToList(),
            AverageRating = ratingValues.Count > 0 ? Math.Round(ratingValues.Average(), 2) : null,
            RatingCount = ratings.Count,
            CommentCount = commentCount,
            FavoriteCount = favoriteCount,
            IsFavorited = isFavorited
        };
    }
}