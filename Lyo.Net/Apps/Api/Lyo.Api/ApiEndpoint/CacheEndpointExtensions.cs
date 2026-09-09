using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Common.Response;
using Lyo.Api.Models.Error;
using Lyo.Api.Services.Cache;
using Lyo.Cache;
using Lyo.Query.Models.Common.Request;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Lyo.Api.ApiEndpoint;

/// <summary>Cache admin Minimal APIs: uncached QueryProject over <see cref="ICacheService.Items" />, clear, and IDeleteService-shaped Delete and Bulk.</summary>
public static class CacheEndpointExtensions
{
    /// <summary>
    /// Maps cache admin endpoints. <see cref="ICacheService.Items" /> is this process's L1 snapshot (Fusion events), not a full Redis dump. QueryProject responses are never
    /// written back into the cache. Keys and tags share <c>DELETE {route}</c> and <c>DELETE {route}/Bulk</c> (<see cref="DeleteRequest" /> composite key Type + Name).
    /// </summary>
    /// <remarks>
    /// These routes enumerate and purge the whole cache, so they require an authenticated caller unless <paramref name="auth" /> explicitly opts out with
    /// <see cref="EndpointAuth.Anonymous" />.
    /// </remarks>
    /// <param name="endpoints">Endpoint route builder (typically <see cref="WebApplication" />).</param>
    /// <param name="baseRoute">Route prefix without a trailing slash (default <c>Cache</c>).</param>
    /// <param name="configure">Optional endpoint conventions applied on top of <paramref name="auth" /> (for example rate limiting, OpenAPI metadata).</param>
    /// <param name="auth">Authorization for every cache route. Defaults to <see cref="EndpointAuth.RequireAuthorization()" />.</param>
    public static IEndpointRouteBuilder MapCacheEndpoints(
        this IEndpointRouteBuilder endpoints, string baseRoute = "Cache", Action<RouteHandlerBuilder>? configure = null, EndpointAuth? auth = null)
    {
        var prefix = string.IsNullOrWhiteSpace(baseRoute) ? "Cache" : baseRoute.Trim().Trim('/');
        var effectiveAuth = auth ?? EndpointAuth.RequireAuthorization();
        Apply(
            endpoints.MapPost(
                    $"{prefix}/QueryProject", (
                        [FromBody] ProjectionQueryReq queryRequest, [FromServices] CacheQueryService cacheQuery, HttpContext httpContext, CancellationToken ct = default) => {
                        WriteNoStore(httpContext);
                        var result = cacheQuery.QueryProjected(queryRequest, ct);
                        if (result.IsSuccess)
                            return Results.Ok(result);

                        return ApiErrorResponseFactory.ThrowForError(httpContext, result.Error);
                    })
                .WithName("QueryProjectCache")
                .WithTags("Cache")
                .Produces<ProjectedQueryRes<object?>>()
                .Produces<LyoProblemDetails>(StatusCodes.Status400BadRequest), configure, effectiveAuth);

        Apply(
            endpoints.MapPost(
                    $"{prefix}/Clear", async ([FromServices] CacheQueryService cacheQuery, HttpContext httpContext, CancellationToken ct = default) => {
                        WriteNoStore(httpContext);
                        return Results.Ok(await cacheQuery.ClearAsync(ct).ConfigureAwait(false));
                    })
                .WithName("ClearCache")
                .WithTags("Cache")
                .Produces<CacheMutationRes>(), configure, effectiveAuth);

        Apply(
            endpoints.MapDelete(
                    $"{prefix}", async (
                        [FromBody] DeleteRequest request, [FromServices] CacheQueryService cacheQuery, HttpContext httpContext, CancellationToken ct = default) => {
                        WriteNoStore(httpContext);
                        var result = await cacheQuery.DeleteAsync(request, ct).ConfigureAwait(false);
                        if (result.IsSuccess)
                            return Results.Ok(result);

                        return ApiErrorResponseFactory.ThrowForError(httpContext, result.Error);
                    })
                .WithName("DeleteCache")
                .WithTags("Cache")
                .Produces<DeleteResult<object?>>()
                .Produces<LyoProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<LyoProblemDetails>(StatusCodes.Status400BadRequest), configure, effectiveAuth);

        Apply(
            endpoints.MapDelete(
                    $"{prefix}/Bulk", async (
                        [FromBody] List<DeleteRequest> requests, [FromServices] CacheQueryService cacheQuery, HttpContext httpContext, CancellationToken ct = default) => {
                        WriteNoStore(httpContext);
                        return Results.Ok(await cacheQuery.DeleteBulkAsync(requests, ct).ConfigureAwait(false));
                    })
                .WithName("DeleteCacheBulk")
                .WithTags("Cache")
                .Produces<DeleteBulkResult<object?>>(), configure, effectiveAuth);

        return endpoints;
    }

    private static void Apply(RouteHandlerBuilder builder, Action<RouteHandlerBuilder>? configure, EndpointAuth auth)
    {
        EndpointAuthorizationApplier.Apply(builder, auth);
        configure?.Invoke(builder);
    }

    private static void WriteNoStore(HttpContext http)
    {
        http.Response.Headers.CacheControl = "no-store, no-cache";
        http.Response.Headers.Pragma = "no-cache";
    }
}
