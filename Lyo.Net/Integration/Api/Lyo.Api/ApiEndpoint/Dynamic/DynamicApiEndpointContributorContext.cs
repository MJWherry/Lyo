using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Error;
using Lyo.Api.Services.Export;
using Lyo.Common.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Api.ApiEndpoint.Dynamic;

internal sealed class DynamicApiEndpointContributorContext<TContext>(
    WebApplication webApp,
    IReadOnlyDictionary<string, EntityEndpointMetadata> registry,
    DynamicEndpointConfig<TContext> config) : IDynamicApiEndpointContributorContext<TContext>
    where TContext : DbContext
{
    public void RegisterExportRoute()
    {
        var entityRoute = config.Defaults.BaseRoute.TrimEnd('/');
        var routePrefix = string.IsNullOrEmpty(entityRoute) ? "" : entityRoute + "/";
        entityRoute = $"{routePrefix}{{entityType}}";
        webApp.MapPost(
                $"{entityRoute}/Export",
                async (
                    [FromRoute] string entityType, [FromBody] ExportRequest request, [FromServices] IExportService<TContext> exportService, HttpContext httpContext,
                    CancellationToken ct) => await DynamicCrudEndpointBuilder.HandleExport(
                    registry, config, entityType, request, exportService, httpContext, SortDirection.Desc, ct))
            .WithTags("Dynamic")
            .Produces(StatusCodes.Status200OK)
            .Produces<LyoProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<LyoProblemDetails>(StatusCodes.Status404NotFound);
    }
}