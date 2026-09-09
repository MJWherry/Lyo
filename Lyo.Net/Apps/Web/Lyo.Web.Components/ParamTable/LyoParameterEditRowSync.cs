using Lyo.Api.Client;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Common.Response;
using Lyo.Parameters;

namespace Lyo.Web.Components.ParamTable;

/// <summary>
/// Persists the edits a user made in a <see cref="LyoParameterEditor" /> back to a CRUD parameter route. The editor hands back a flat row list with no change log, so saving means
/// reconciling it against what was loaded: rows that disappeared are deleted, new rows are created, and the rest are updated.
/// </summary>
public static class LyoParameterEditRowSync
{
    /// <summary>
    /// Reconciles <paramref name="rows" /> against <paramref name="loadedIds" /> and writes the differences. Deletes run first so a row can be replaced by a new one with the same
    /// key without tripping a uniqueness constraint. Created rows are stamped with their server id, so calling this twice does not duplicate them.
    /// </summary>
    /// <typeparam name="TRequest">Request DTO for this parameter kind, for example <c>JobParameterReq</c>.</typeparam>
    /// <typeparam name="TResponse">Response DTO for this parameter kind.</typeparam>
    /// <param name="apiClient">Client for the owning API.</param>
    /// <param name="parameterRoute">CRUD route for the parameter entity, for example <c>Job/Definition/Parameter</c>.</param>
    /// <param name="rows">Rows currently in the editor.</param>
    /// <param name="loadedIds">Ids that were loaded into the editor. Any id missing from <paramref name="rows" /> is deleted.</param>
    /// <param name="toRequest">Converts a row to the request DTO, filling in the owning definition id that only the caller knows.</param>
    /// <param name="idOf">Reads the id off a create response.</param>
    /// <param name="ct">Cancellation token.</param>
    public static async Task SaveAsync<TRequest, TResponse>(
        IApiClient apiClient,
        string parameterRoute,
        IReadOnlyList<LyoParameterEditRow> rows,
        IEnumerable<Guid> loadedIds,
        Func<LyoParameterEditRow, TRequest> toRequest,
        Func<TResponse, Guid> idOf,
        CancellationToken ct = default)
        where TRequest : LyoParameterDefinitionBase
        where TResponse : class, ILyoParameterDefinition
    {
        var route = parameterRoute.TrimEnd('/');
        foreach (var removed in loadedIds.Where(id => rows.All(r => r.Id != id)))
            await apiClient.DeleteAsAsync<object>($"{route}/{removed}", ct: ct);

        foreach (var added in rows.Where(r => r.IsNew)) {
            var created = await apiClient.PostAsAsync<TRequest, CreateResult<TResponse>>(route, toRequest(added), ct: ct);
            if (created?.Data is { } data)
                added.ApplyCreated(idOf(data), data.Options, data.AllowedValues, data.Value);
        }

        foreach (var updated in rows.Where(r => !r.IsNew && r.Id.HasValue))
            await apiClient.PostAsAsync<UpdateRequest<TRequest>, UpdateResult<TResponse>>($"{route}/Update", new(toRequest(updated), updated.Id!.Value), ct: ct);
    }
}
