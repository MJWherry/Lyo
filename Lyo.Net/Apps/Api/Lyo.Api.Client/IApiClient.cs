using Lyo.Http.Client;
using Lyo.Query.Models.Common.Request;

namespace Lyo.Api.Client;

/// <summary>Lyo API HTTP client: problem-details, routes, Query/QueryProject. Generic JSON verbs live on <see cref="ILyoHttpClient" />.</summary>
public interface IApiClient : ILyoHttpClient
{
    /// <summary>POST <c>{route}/QueryProject</c> with a projection request.</summary>
    Task<TResult?> QueryProjectAsync<TResult>(string route, ProjectionQueryReq request, Action<HttpRequestMessage>? before = null, CancellationToken ct = default);

    /// <summary>POST <c>{route}/QueryConcrete</c> with a concrete query request.</summary>
    Task<TResult?> QueryConcreteAsync<TResult>(string route, QueryConcreteReq request, Action<HttpRequestMessage>? before = null, CancellationToken ct = default);
}
