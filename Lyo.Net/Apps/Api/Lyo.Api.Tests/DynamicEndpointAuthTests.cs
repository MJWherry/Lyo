using Lyo.Api.ApiEndpoint;
using Lyo.Api.ApiEndpoint.Dynamic;
using Lyo.Cache;
using Lyo.Job.Models.Response;
using Lyo.Job.Postgres;
using Lyo.Job.Postgres.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Api.Tests;

/// <summary>
/// The dynamic CRUD builder ignored the configured <see cref="EndpointAuth" /> entirely, so every dynamic route. including delete and the entity metadata reflection routes.
/// was anonymous. Registration now fails unless the host states its intent.
/// </summary>
public sealed class DynamicEndpointAuthTests
{
    [Fact]
    public void MapDynamicCrudEndpoints_WithNoAuthConfigured_ThrowsAtRegistration()
    {
        var app = CreateApp();
        var exception = Record.Exception(() => app.MapDynamicCrudEndpoints<JobContext>(c => c.WithDefaults(d => d.BaseRoute = "api/Job").IncludeOnly<JobDefinition>()));
        Assert.IsType<InvalidOperationException>(exception);
        Assert.Contains("AllowAnonymous", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MapDynamicCrudEndpoints_WithAllowAnonymous_LeavesRoutesAnonymous()
    {
        var app = CreateApp();
        app.MapDynamicCrudEndpoints<JobContext>(c => c.AllowAnonymous().WithDefaults(d => d.BaseRoute = "api/Job").IncludeOnly<JobDefinition>());
        Assert.All(DynamicEndpoints(app), endpoint => Assert.NotNull(endpoint.Metadata.GetMetadata<IAllowAnonymous>()));
    }

    [Fact]
    public void MapDynamicCrudEndpoints_WithRequireAuthorization_AuthorizesEveryRoute()
    {
        var app = CreateApp();
        app.MapDynamicCrudEndpoints<JobContext>(c => c.RequireAuthorization().WithDefaults(d => d.BaseRoute = "api/Job").IncludeOnly<JobDefinition>());
        var endpoints = DynamicEndpoints(app);
        Assert.NotEmpty(endpoints);
        Assert.All(endpoints, endpoint => Assert.NotNull(endpoint.Metadata.GetMetadata<IAuthorizeData>()));
    }

    /// <summary>The entity metadata routes reflect every public property of every registered entity, so they must be gated with the rest.</summary>
    [Fact]
    public void MapDynamicCrudEndpoints_WithRequireAuthorization_AuthorizesMetadataRoutes()
    {
        var app = CreateApp();
        app.MapDynamicCrudEndpoints<JobContext>(c => c.RequireAuthorization().WithDefaults(d => d.BaseRoute = "api/Job").IncludeOnly<JobDefinition>());
        var metadataRoutes = DynamicEndpoints(app).Where(e => RoutePatternOf(e).Contains("Metadata", StringComparison.OrdinalIgnoreCase)).ToList();
        Assert.NotEmpty(metadataRoutes);
        Assert.All(metadataRoutes, endpoint => Assert.NotNull(endpoint.Metadata.GetMetadata<IAuthorizeData>()));
    }

    /// <summary>Root <c>POST /Query</c> reaches every allowlisted entity, so the dynamic base must thread its auth through to it.</summary>
    [Fact]
    public void MapDynamicCrudEndpoints_WithRequireAuthorization_AuthorizesRootQueryRoute()
    {
        var app = CreateApp();
        app.MapDynamicCrudEndpoints<JobContext>(c => c.RequireAuthorization().WithDefaults(d => d.BaseRoute = "api/Job").IncludeOnly<JobDefinition>());
        var rootQuery = DynamicEndpoints(app).Single(e => RoutePatternOf(e).Equals("api/Job/Query", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(rootQuery.Metadata.GetMetadata<IAuthorizeData>());
    }

    [Fact]
    public void MapRootQueryEndpoints_WithNoConfigure_ThrowsAtRegistration()
    {
        var app = CreateApp();
        var exception = Record.Exception(() => app.MapRootQueryEndpoints<JobContext>());
        Assert.IsType<InvalidOperationException>(exception);
    }

    private static WebApplication CreateApp()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddLocalCache();
        builder.Services.AddLyoQueryServices();
        builder.Services.AddAuthorization();
        builder.Services.AddPostgresJobManagement(
            new PostgresJobOptions { ConnectionString = "Host=localhost;Database=test;Username=test;Password=test", EnableAutoMigrations = false });

        return builder.Build();
    }

    private static List<Endpoint> DynamicEndpoints(WebApplication app)
        => [.. ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).Where(e => RoutePatternOf(e).Length > 0)];

    private static string RoutePatternOf(Endpoint endpoint) => endpoint is RouteEndpoint route ? route.RoutePattern.RawText ?? "" : "";
}
