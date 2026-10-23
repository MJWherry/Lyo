using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Lyo.FeatureFlags.AspNetCore.Tests;

public sealed class FeatureFlagMiddlewareTests
{
    [Fact]
    public async Task RequireFeature_Enabled_Returns200()
    {
        await using var host = await HostAsync(app => app.MapGet("/on", Hit).RequireFeature("on"), o => o.Add("on").On());
        var response = await SendAsync(host, "/on");
        Assert.Equal(StatusCodes.Status200OK, response.Status);
        Assert.Equal(1, Runs(host));
    }

    [Fact]
    public async Task RequireFeature_Disabled_Returns404EmptyBodyAndDoesNotRun()
    {
        await using var host = await HostAsync(app => app.MapGet("/off", Hit).RequireFeature("off"), o => o.Add("off"));
        var response = await SendAsync(host, "/off");
        Assert.Equal(StatusCodes.Status404NotFound, response.Status);
        Assert.Equal(string.Empty, response.Body);
        Assert.Equal(0, Runs(host));
    }

    [Fact]
    public async Task OpenRoute_WithoutService_Returns200()
    {
        await using var host = await HostAsync(app => app.MapGet("/open", Hit), registerService: false);
        var response = await SendAsync(host, "/open");
        Assert.Equal(StatusCodes.Status200OK, response.Status);
        Assert.Equal(1, Runs(host));
    }

    [Fact]
    public async Task RequireFeature_MissingService_Returns500EmptyBodyAndDoesNotRun()
    {
        await using var host = await HostAsync(app => app.MapGet("/off", Hit).RequireFeature("off"), registerService: false);
        var response = await SendAsync(host, "/off");
        Assert.Equal(StatusCodes.Status500InternalServerError, response.Status);
        Assert.Equal(string.Empty, response.Body);
        Assert.Equal(0, Runs(host));
    }

    [Fact]
    public async Task Attribute_OnMethod_Disabled_Returns404()
    {
        await using var host = await HostAsync(app => app.MapGet("/method", FlaggedEndpoints.Off));
        var response = await SendAsync(host, "/method");
        Assert.Equal(StatusCodes.Status404NotFound, response.Status);
        Assert.Equal(0, Runs(host));
    }

    [Fact]
    public async Task Fluent_OnLambda_Disabled_Returns404()
    {
        await using var host = await HostAsync(app => app.MapGet(
                "/lambda", (RunCounter counter) => {
                    counter.Runs++;
                    return Results.Ok("ok");
                })
            .RequireFeature("off"));

        var response = await SendAsync(host, "/lambda");
        Assert.Equal(StatusCodes.Status404NotFound, response.Status);
        Assert.Equal(0, Runs(host));
    }

    [Fact]
    public async Task MapGroup_RequireFeature_Disabled_Returns404()
    {
        await using var host = await HostAsync(app => app.MapGroup("/g").RequireFeature("off").MapGet("/x", Hit));
        var response = await SendAsync(host, "/g/x");
        Assert.Equal(StatusCodes.Status404NotFound, response.Status);
        Assert.Equal(0, Runs(host));
    }

    [Fact]
    public async Task Attribute_OnControllerClass_Disabled_Returns404()
    {
        await using var host = await HostAsync(app => app.MapControllers(), controllers: true);
        var response = await SendAsync(host, "/ctrl/class");
        Assert.Equal(StatusCodes.Status404NotFound, response.Status);
        Assert.Equal(string.Empty, response.Body);
        Assert.Equal(0, Runs(host));
    }

    [Fact]
    public async Task RequireFeature_TwoFlags_OneOff_Returns404()
    {
        await using var host = await HostAsync(app => app.MapGet("/both", Hit).RequireFeature("on").RequireFeature("off"), o => o.Add("on").On());
        var response = await SendAsync(host, "/both");
        Assert.Equal(StatusCodes.Status404NotFound, response.Status);
        Assert.Equal(0, Runs(host));
    }

    [Fact]
    public async Task ClaimRule_LyoUserMatch_Returns200()
    {
        await using var host = await HostAsync(app => app.MapGet("/me", Hit).RequireFeature("me"), o => o.Add("me").WhenUser("user-a"));
        var response = await SendAsync(host, "/me", "user-a");
        Assert.Equal(StatusCodes.Status200OK, response.Status);
    }

    [Fact]
    public async Task ClaimRule_WrongUser_Returns404()
    {
        await using var host = await HostAsync(app => app.MapGet("/me", Hit).RequireFeature("me"), o => o.Add("me").WhenUser("user-a"));
        var response = await SendAsync(host, "/me", "user-b");
        Assert.Equal(StatusCodes.Status404NotFound, response.Status);
        Assert.Equal(0, Runs(host));
    }

    [Fact]
    public async Task ClaimRule_ExtraClaims_StillMatches()
    {
        await using var host = await HostAsync(app => app.MapGet("/role", Hit).RequireFeature("role"), o => o.Add("role").WhenClaim("role", "admin"));
        var response = await SendAsync(host, "/role", "user-a", "admin");
        Assert.Equal(StatusCodes.Status200OK, response.Status);
    }

    [Fact]
    public async Task ScopeClaim_SpaceDelimited_MatchesOneScope()
    {
        await using var host = await HostAsync(app => app.MapGet("/scope", Hit).RequireFeature("read"), o => o.Add("read").WhenClaim("scope", "reports:read"));
        var response = await SendAsync(host, "/scope", scope: "reports:read write");
        Assert.Equal(StatusCodes.Status200OK, response.Status);
    }

    [Fact]
    public async Task ScopeClaim_CombinedValue_DoesNotMatch()
    {
        await using var host = await HostAsync(app => app.MapGet("/scope", Hit).RequireFeature("both"), o => o.Add("both").WhenClaim("scope", "reports:read write"));
        var response = await SendAsync(host, "/scope", scope: "reports:read write");
        Assert.Equal(StatusCodes.Status404NotFound, response.Status);
        Assert.Equal(0, Runs(host));
    }

    [Fact]
    public async Task TenantClaimType_WhenSet_MatchesTenantRule()
    {
        await using var host = await HostAsync(
            app => app.MapGet("/tenant", Hit).RequireFeature("acme"), o => o.Add("acme").WhenTenant("acme"), endpoint => endpoint.TenantClaimType = "tenant_id");

        var response = await SendAsync(host, "/tenant", tenant: "acme");
        Assert.Equal(StatusCodes.Status200OK, response.Status);
    }

    [Fact]
    public async Task TenantClaimType_Unset_TenantRule_Returns404()
    {
        await using var host = await HostAsync(app => app.MapGet("/tenant", Hit).RequireFeature("acme"), o => o.Add("acme").WhenTenant("acme"));
        var response = await SendAsync(host, "/tenant", tenant: "acme");
        Assert.Equal(StatusCodes.Status404NotFound, response.Status);
        Assert.Equal(0, Runs(host));
    }

    [Fact]
    public async Task AttributeRule_WithoutFactory_Returns404()
    {
        await using var host = await HostAsync(app => app.MapGet("/plan", Hit).RequireFeature("plan"), o => o.Add("plan").WhenAttribute("plan", "pro"));
        var response = await SendAsync(host, "/plan", "user-a");
        Assert.Equal(StatusCodes.Status404NotFound, response.Status);
        Assert.Equal(0, Runs(host));
    }

    [Fact]
    public async Task ContextFactory_ReplacesDefaultMapping_Returns404()
    {
        await using var replaced = await HostAsync(
            app => app.MapGet("/me", Hit).RequireFeature("me"), o => o.Add("me").WhenUser("header-user"),
            endpoint => endpoint.ContextFactory = _ => FeatureFlagContext.Create().WithUser("someone-else").Build());

        var missed = await SendAsync(replaced, "/me", "header-user");
        Assert.Equal(StatusCodes.Status404NotFound, missed.Status);
        Assert.Equal(0, Runs(replaced));
        await using var empty = await HostAsync(
            app => app.MapGet("/me", Hit).RequireFeature("me"), o => o.Add("me").WhenUser("header-user"), endpoint => endpoint.ContextFactory = _ => null!);

        var nulled = await SendAsync(empty, "/me", "header-user");
        Assert.Equal(StatusCodes.Status404NotFound, nulled.Status);
        await using var defaults = await HostAsync(app => app.MapGet("/me", Hit).RequireFeature("me"), o => o.Add("me").WhenUser("header-user"));
        var matched = await SendAsync(defaults, "/me", "header-user");
        Assert.Equal(StatusCodes.Status200OK, matched.Status);
    }

    [Fact]
    public async Task MapFeatureFlags_ReturnsEnabledNamesOnly()
    {
        await using var host = await HostAsync(
            app => app.MapFeatureFlags(), o => {
                o.Add("on").On();
                o.Add("off");
            });

        var response = await SendAsync(host, "/features");
        Assert.Equal(StatusCodes.Status200OK, response.Status);
        Assert.Equal("[\"on\"]", response.Body);
    }

    [Fact]
    public async Task MapFeatureFlags_UsesLyoUserClaim()
    {
        await using var host = await HostAsync(app => app.MapFeatureFlags(), o => o.Add("me").WhenUser("user-a"));
        var miss = await SendAsync(host, "/features");
        Assert.Equal(StatusCodes.Status200OK, miss.Status);
        Assert.Equal("[]", miss.Body);
        var hit = await SendAsync(host, "/features", "user-a");
        Assert.Equal("[\"me\"]", hit.Body);
    }

    [Fact]
    public async Task MapFeatureFlags_MissingService_Returns500EmptyBody()
    {
        await using var host = await HostAsync(app => app.MapFeatureFlags(), registerService: false);
        var response = await SendAsync(host, "/features");
        Assert.Equal(StatusCodes.Status500InternalServerError, response.Status);
        Assert.Equal(string.Empty, response.Body);
    }

    [Fact]
    public async Task MapFeatureFlagManagement_ListsModes()
    {
        await using var host = await HostAsync(
            app => {
                app.MapFeatureFlags();
                app.MapFeatureFlagManagement();
            }, o => {
                o.Add("on").On();
                o.Add("off");
            });

        var manage = await SendAsync(host, "/features/manage");
        Assert.Equal(StatusCodes.Status200OK, manage.Status);
        Assert.Contains("\"name\":\"on\"", manage.Body, StringComparison.Ordinal);
        Assert.Contains("\"mode\":\"On\"", manage.Body, StringComparison.Ordinal);
        Assert.Contains("\"name\":\"off\"", manage.Body, StringComparison.Ordinal);
        Assert.Contains("\"mode\":\"Off\"", manage.Body, StringComparison.Ordinal);
        var enabled = await SendAsync(host, "/features");
        Assert.Equal("[\"on\"]", enabled.Body);
    }

    [Fact]
    public async Task MapFeatureFlagManagement_SetEnabled_FlipsFlag()
    {
        await using var host = await HostAsync(app => app.MapFeatureFlagManagement(), o => o.Add("sample").On());
        var response = await PutAsync(host, "/features/manage/sample", """{"enabled":false}""");
        Assert.Equal(StatusCodes.Status200OK, response.Status);
        Assert.Contains("\"mode\":\"Off\"", response.Body, StringComparison.Ordinal);
        Assert.False(host.Services.GetRequiredService<IFeatureFlagService>().IsEnabled("sample"));
        var manage = await SendAsync(host, "/features/manage");
        Assert.Contains("\"mode\":\"Off\"", manage.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MapFeatureFlagManagement_UnknownName_Returns404()
    {
        await using var host = await HostAsync(app => app.MapFeatureFlagManagement(), o => o.Add("sample").On());
        var response = await PutAsync(host, "/features/manage/missing", """{"enabled":true}""");
        Assert.Equal(StatusCodes.Status404NotFound, response.Status);
        Assert.Equal(string.Empty, response.Body);
        Assert.True(host.Services.GetRequiredService<IFeatureFlagService>().IsEnabled("sample"));
    }

    [Fact]
    public async Task MapFeatureFlagManagement_MissingService_Returns500EmptyBody()
    {
        await using var host = await HostAsync(app => app.MapFeatureFlagManagement(), registerService: false);
        var response = await SendAsync(host, "/features/manage");
        Assert.Equal(StatusCodes.Status500InternalServerError, response.Status);
        Assert.Equal(string.Empty, response.Body);
    }

    private static IResult Hit(RunCounter counter)
    {
        counter.Runs++;
        return Results.Ok("ok");
    }

    private static int Runs(WebApplication host) => host.Services.GetRequiredService<RunCounter>().Runs;

    private static async Task<WebApplication> HostAsync(
        Action<WebApplication> map,
        Action<FeatureFlagOptions>? flags = null,
        Action<FeatureFlagEndpointOptions>? endpoint = null,
        bool controllers = false,
        bool registerService = true)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Services.AddSingleton<RunCounter>();
        if (registerService)
            builder.Services.AddFeatureFlags(flags);

        if (controllers)
            builder.Services.AddControllers().AddApplicationPart(typeof(FlaggedController).Assembly);

        var app = builder.Build();
        app.UseRouting();
        app.Use(async (context, next) => {
            var claims = new List<Claim>();
            AddClaim(claims, "lyo:user", context.Request.Headers["X-Test-Lyo-User"]);
            AddClaim(claims, "sub", context.Request.Headers["X-Test-Sub"]);
            AddClaim(claims, "role", context.Request.Headers["X-Test-Role"]);
            AddClaim(claims, "scope", context.Request.Headers["X-Test-Scope"]);
            AddClaim(claims, "tenant_id", context.Request.Headers["X-Test-Tenant"]);
            if (claims.Count > 0)
                context.User = new(new ClaimsIdentity(claims, "test"));

            await next(context);
        });

        app.UseFeatureFlags(endpoint);
        map(app);
        await app.StartAsync(TestCt);
        return app;
    }

    private static void AddClaim(List<Claim> claims, string type, StringValues value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            claims.Add(new(type, value.ToString()));
    }

    private static async Task<Captured> SendAsync(WebApplication host, string path, string? lyoUser = null, string? role = null, string? scope = null, string? tenant = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (lyoUser is not null)
            request.Headers.TryAddWithoutValidation("X-Test-Lyo-User", lyoUser);

        if (role is not null)
            request.Headers.TryAddWithoutValidation("X-Test-Role", role);

        if (scope is not null)
            request.Headers.TryAddWithoutValidation("X-Test-Scope", scope);

        if (tenant is not null)
            request.Headers.TryAddWithoutValidation("X-Test-Tenant", tenant);

        var response = await host.GetTestClient().SendAsync(request, TestCt);
        var body = await response.Content.ReadAsStringAsync(TestCt);
        return new((int)response.StatusCode, body);
    }

    private static async Task<Captured> PutAsync(WebApplication host, string path, string json)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, path) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        var response = await host.GetTestClient().SendAsync(request, TestCt);
        var body = await response.Content.ReadAsStringAsync(TestCt);
        return new((int)response.StatusCode, body);
    }

    private sealed record Captured(int Status, string Body);
}

public sealed class RunCounter
{
    public int Runs { get; set; }
}

public static class FlaggedEndpoints
{
    [RequireFeature("off")]
    public static IResult Off(RunCounter counter)
    {
        counter.Runs++;
        return Results.Ok("ok");
    }
}

[ApiController]
[Route("ctrl")]
[RequireFeature("off")]
public sealed class FlaggedController : ControllerBase
{
    [HttpGet("class")]
    public IActionResult ClassLevel([FromServices] RunCounter counter)
    {
        counter.Runs++;
        return Ok("ok");
    }
}