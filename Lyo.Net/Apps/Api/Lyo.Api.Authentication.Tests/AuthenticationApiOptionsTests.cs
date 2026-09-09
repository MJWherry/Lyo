using Lyo.Api.ApiEndpoint;
using Lyo.Authentication.Postgres;
using Lyo.Cache;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Api.Authentication.Tests;

public sealed class AuthenticationApiOptionsTests
{
    [Fact]
    public void Defaults_RequireAuthorization()
    {
        var options = new AuthenticationApiOptions();
        Assert.False(options.UserAuth?.AllowAnonymous);
        Assert.False(options.TokenAuth?.AllowAnonymous);
        Assert.False(options.ClaimAuth?.AllowAnonymous);
        Assert.False(options.ScopeAuth?.AllowAnonymous);
        Assert.False(options.LinkedIdentityAuth?.AllowAnonymous);
        Assert.False(options.EventAuth?.AllowAnonymous);
    }

    [Fact]
    public void WithAuth_AppliesToEverySurface()
    {
        var options = AuthenticationApiOptions.WithAuth(EndpointAuth.Anonymous());
        Assert.True(options.UserAuth?.AllowAnonymous);
        Assert.True(options.TokenAuth?.AllowAnonymous);
        Assert.True(options.ClaimAuth?.AllowAnonymous);
        Assert.True(options.ScopeAuth?.AllowAnonymous);
        Assert.True(options.LinkedIdentityAuth?.AllowAnonymous);
        Assert.True(options.EventAuth?.AllowAnonymous);
    }

    [Fact]
    public void BuildAuthenticationApi_RequireAuthorization_AuthorizesMappedRoutes()
    {
        var app = CreateApp();
        app.BuildAuthenticationApi();
        var endpoints = MappedAuthEndpoints(app);
        Assert.NotEmpty(endpoints);
        Assert.All(endpoints, endpoint => Assert.NotNull(endpoint.Metadata.GetMetadata<IAuthorizeData>()));
        Assert.All(endpoints, endpoint => Assert.Null(endpoint.Metadata.GetMetadata<IAllowAnonymous>()));
    }

    [Fact]
    public void BuildAuthenticationApi_Anonymous_LeavesRoutesAnonymous()
    {
        var app = CreateApp();
        app.BuildAuthenticationApi(AuthenticationApiOptions.WithAuth(EndpointAuth.Anonymous()));
        var endpoints = MappedAuthEndpoints(app);
        Assert.NotEmpty(endpoints);
        Assert.All(endpoints, endpoint => Assert.NotNull(endpoint.Metadata.GetMetadata<IAllowAnonymous>()));
    }

    private static WebApplication CreateApp()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddLocalCache();
        builder.Services.AddLyoQueryServices();
        builder.Services.AddAuthorization();
        builder.Services.AddPostgresAuthenticationStores(o => {
            o.ConnectionString = "Host=localhost;Database=test;Username=test;Password=test";
            o.EnableAutoMigrations = false;
        });
        builder.Services.AddLyoApiAuthentication();
        builder.Services.AddScoped<Lyo.Api.Mapping.ILyoMapper>(sp => sp.GetRequiredService<AuthenticationLyoMapper>());
        return builder.Build();
    }

    private static List<Endpoint> MappedAuthEndpoints(WebApplication app)
        => [.. ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints)
            .Where(e => RoutePatternOf(e).Contains("Auth/", StringComparison.OrdinalIgnoreCase) || RoutePatternOf(e).StartsWith("Auth", StringComparison.OrdinalIgnoreCase))];

    private static string RoutePatternOf(Endpoint endpoint)
        => endpoint is RouteEndpoint route ? route.RoutePattern.RawText ?? "" : endpoint.DisplayName ?? "";
}
