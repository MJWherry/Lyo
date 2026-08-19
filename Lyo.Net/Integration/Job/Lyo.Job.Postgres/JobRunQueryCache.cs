using Lyo.Api.Services.Crud.Read.Query;
using Lyo.Cache;
using Lyo.Job.Postgres.Database;

namespace Lyo.Job.Postgres;

/// <summary>
/// Busts cached JobRun list/GET pages and JobDefinition queries that project last-run columns. Definition grids select
/// <c>JobRuns.CreatedTimestamp</c> without tagging <c>entity:jobrun</c>, so run creates/deletes must also clear <c>entity:jobdefinition</c>.
/// </summary>
internal static class JobRunQueryCache
{
    public static Task InvalidateAsync(ICacheService? cache)
    {
        if (cache is null)
            return Task.CompletedTask;

        return Task.WhenAll(
            QueryCacheInvalidation.InvalidateQueryCachesForBroadEntityTypeAsync<JobRun>(cache),
            QueryCacheInvalidation.InvalidateQueryCachesForBroadEntityTypeAsync<JobDefinition>(cache));
    }
}
