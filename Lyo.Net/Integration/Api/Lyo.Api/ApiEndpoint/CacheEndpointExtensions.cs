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

/// <summary>Cache admin Minimal APIs: uncached QueryProject over <see cref="ICacheService.Items" />, clear, and IDeleteService-shaped Delete / Bulk.</summary>
public static class CacheEndpointExtensions
{
    /// <summary>
    /// Maps cache admin endpoints. <see cref="ICacheService.Items" /> is this process’s L1 snapshot (Fusion events), not a full Redis dump. QueryProject responses are never
    /// written back into the cache. Keys and tags share <c>DELETE {route}</c> and <c>DELETE {route}/Bulk</c> (<see cref="DeleteRequest" /> composite key Type + Name).
    /// </summary>
    /// <param name="endpoints">The endpoint route builder (typically <see cref="WebApplication" />).</param>
    /// <param name="baseRoute">Route prefix without trailing slash (default <c>Cache</c>).</param>
    /// <param name="configure">Optional endpoint conventions (e.g. <c>b => b.RequireAuthorization()</c>).</param>
    public static IEndpointRouteBuilder MapCacheEndpoints(this IEndpointRouteBuilder endpoints, string baseRoute = "Cache", Action<RouteHandlerBuilder>? configure = null)
    {
        var prefix = string.IsNullOrWhiteSpace(baseRoute) ? "Cache" : baseRoute.Trim().Trim('/');
        Apply(
            endpoints.MapPost(
                    $"{prefix}/QueryProject", (
                        [FromBody] ProjectionQueryReq queryRequest, [FromServices] CacheQueryService cacheQuery, HttpContext httpContext) => {
                        WriteNoStore(httpContext);
                        var result = cacheQuery.QueryProjected(queryRequest);
                        if (result.IsSuccess)
                            return Results.Ok(result);

                        return ApiErrorResponseFactory.ThrowForError(httpContext, result.Error);
                    })
                .WithName("QueryProjectCache")
                .WithTags("Cache")
                .Produces<ProjectedQueryRes<object?>>()
                .Produces<LyoProblemDetails>(StatusCodes.Status400BadRequest), configure);

        Apply(
            endpoints.MapPost(
                    $"{prefix}/Clear", async ([FromServices] CacheQueryService cacheQuery, HttpContext httpContext) => {
                        WriteNoStore(httpContext);
                        return Results.Ok(await cacheQuery.ClearAsync().ConfigureAwait(false));
                    })
                .WithName("ClearCache")
                .WithTags("Cache")
                .Produces<CacheMutationRes>(), configure);

        Apply(
            endpoints.MapDelete(
                    $"{prefix}", async ([FromBody] DeleteRequest request, [FromServices] CacheQueryService cacheQuery, HttpContext httpContext) => {
                        WriteNoStore(httpContext);
                        var result = await cacheQuery.DeleteAsync(request).ConfigureAwait(false);
                        if (result.IsSuccess)
                            return Results.Ok(result);

                        return ApiErrorResponseFactory.ThrowForError(httpContext, result.Error);
                    })
                .WithName("DeleteCache")
                .WithTags("Cache")
                .Produces<DeleteResult<object?>>()
                .Produces<LyoProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<LyoProblemDetails>(StatusCodes.Status400BadRequest), configure);

        Apply(
            endpoints.MapDelete(
                    $"{prefix}/Bulk", async ([FromBody] List<DeleteRequest> requests, [FromServices] CacheQueryService cacheQuery, HttpContext httpContext) => {
                        WriteNoStore(httpContext);
                        return Results.Ok(await cacheQuery.DeleteBulkAsync(requests).ConfigureAwait(false));
                    })
                .WithName("DeleteCacheBulk")
                .WithTags("Cache")
                .Produces<DeleteBulkResult<object?>>(), configure);

        return endpoints;
    }

    private static void Apply(RouteHandlerBuilder builder, Action<RouteHandlerBuilder>? configure) => configure?.Invoke(builder);

    private static void WriteNoStore(HttpContext http)
    {
        http.Response.Headers.CacheControl = "no-store, no-cache";
        http.Response.Headers.Pragma = "no-cache";
    }
}
