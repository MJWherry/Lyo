using Lyo.Api.Client;
using Lyo.Api.Models.Common.Response;
using Lyo.Exceptions;
using Lyo.Query.Models.Builders;
using Lyo.Query.Models.Common.Request;
using Microsoft.Extensions.Logging;

namespace Lyo.Web.Components.DataGrid;

/// <summary>
/// After a parent grid page loads, batches related QueryProject calls (Keys = FKs on the page) and fills <see cref="RelatedEntityLookup" />.
/// </summary>
public static class RelatedEntityLoader
{
    /// <summary>
    /// Clears <paramref name="lookup" />, groups visible related columns by route and type, and QueryProjects each group's ids in chunks of
    /// <see cref="RelatedProjection.DefaultKeyChunkSize" />. Related failures are logged and skipped so the parent grid still renders.
    /// </summary>
    public static async Task LoadAsync(
        RelatedEntityLookup lookup,
        RelatedColumnRegistry registry,
        IEnumerable<object?> rows,
        IApiClient defaultClient,
        ILogger logger,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(lookup);
        ArgumentHelpers.ThrowIfNull(registry);
        ArgumentHelpers.ThrowIfNull(rows);
        ArgumentHelpers.ThrowIfNull(defaultClient);
        ArgumentHelpers.ThrowIfNull(logger);

        lookup.Clear();
        var rowList = rows as IReadOnlyList<object?> ?? rows.ToList();
        foreach (var group in RelatedProjection.GroupVisible(registry)) {
            ct.ThrowIfCancellationRequested();
            var ids = RelatedProjection.CollectIds(rowList, group.Fields);
            if (ids.Count == 0)
                continue;

            var client = group.ApiClient ?? defaultClient;
            foreach (var chunk in RelatedProjection.ChunkKeys(ids)) {
                ct.ThrowIfCancellationRequested();
                try {
                    await LoadChunkAsync(lookup, group, chunk, client, logger, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException) {
                    throw;
                }
                catch (Exception ex) {
                    logger.LogWarning(ex, "Related QueryProject failed for {Route}.", group.Route);
                }
            }
        }
    }

    private static async Task LoadChunkAsync(
        RelatedEntityLookup lookup,
        RelatedLoadGroup group,
        IReadOnlyList<object[]> keys,
        IApiClient client,
        ILogger logger,
        CancellationToken ct)
    {
        var req = ProjectionQueryReqBuilder.New()
            .AddSelects(group.Select.ToArray())
            .AddKeys(keys)
            .SetPagination(0, keys.Count)
            .Build();
        var route = group.Route.TrimEnd('/') + "/QueryProject";
        var result = await client.PostAsAsync<ProjectionQueryReq, ProjectedQueryRes<object?>>(route, req, ct: ct).ConfigureAwait(false);
        if (result is not { IsSuccess: true } || result.Items is null) {
            logger.LogWarning("Related QueryProject for {Route} returned {Error}.", group.Route, result?.Error?.GetFullMessage() ?? "no rows");
            return;
        }

        foreach (var item in result.Items) {
            var related = RelatedProjection.Deserialize(item, group.ResType);
            if (related is null)
                continue;

            var id = RelatedProjection.GetFieldValue(related, "Id") ?? RelatedProjection.GetFieldValue(item, "Id");
            if (id is null)
                continue;

            lookup.Set(group.Route, group.ResType, id, related);
        }
    }
}
