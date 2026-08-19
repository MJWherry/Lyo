using Lyo.Api.Services.Crud.Read.Query;
using Lyo.Cache;
using Lyo.Reporting.Postgres.Database;

namespace Lyo.Reporting.Postgres;

/// <summary>
/// Busts cached ReportGeneration list/GET pages and ReportDefinition queries that project last-generation columns. Definition grids select
/// <c>Generations.CreatedTimestamp</c> without tagging <c>entity:reportgeneration</c>, so generate must also clear <c>entity:reportdefinition</c>.
/// </summary>
internal static class ReportGenerationQueryCache
{
    public static Task InvalidateAsync(ICacheService? cache)
    {
        if (cache is null)
            return Task.CompletedTask;

        return Task.WhenAll(
            QueryCacheInvalidation.InvalidateQueryCachesForBroadEntityTypeAsync<ReportGeneration>(cache),
            QueryCacheInvalidation.InvalidateQueryCachesForBroadEntityTypeAsync<ReportDefinition>(cache));
    }
}
