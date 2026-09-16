using System.Net;
using System.Security.Claims;
using Lyo.Api.Middleware;
using Lyo.RateLimit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

namespace Lyo.Api.Tests.RateLimit;

public sealed class RequireRateLimitTests
{
    [Fact]
    public async Task Invoke_UnderLimit_Returns200()
    {
        using var host = await StartHostAsync();
        var client = host.GetTestClient();
        for (var i = 0; i < 4; i++) {
            var response = await client.GetAsync("/under", TestCt);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Fact]
    public async Task Invoke_OverLimit_Returns429WithRetryAfter()
    {
        using var host = await StartHostAsync();
        var client = host.GetTestClient();
        for (var i = 0; i < 4; i++)
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/over", TestCt)).StatusCode);

        var denied = await client.GetAsync("/over", TestCt);
        Assert.Equal((HttpStatusCode)429, denied.StatusCode);
        var seconds = denied.Headers.RetryAfter?.Delta;
        Assert.NotNull(seconds);
        Assert.InRange(seconds.Value.TotalSeconds, 1, 10);
    }

    [Fact]
    public async Task Invoke_RouteWithoutPolicy_IsNotLimited()
    {
        using var host = await StartHostAsync();
        var client = host.GetTestClient();
        for (var i = 0; i < 6; i++)
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/open", TestCt)).StatusCode);
    }

    [Fact]
    public async Task Invoke_SubOnlyPrincipal_DoesNotShareIpBucket()
    {
        using var host = await StartHostAsync();
        var client = host.GetTestClient();
        using var first = new HttpRequestMessage(HttpMethod.Get, "/identity");
        first.Headers.TryAddWithoutValidation("X-Ip", "10.0.0.8");
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(first, TestCt)).StatusCode);

        using var named = new HttpRequestMessage(HttpMethod.Get, "/identity");
        named.Headers.TryAddWithoutValidation("X-Ip", "10.0.0.8");
        named.Headers.TryAddWithoutValidation("X-Sub", "user-1");
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(named, TestCt)).StatusCode);

        using var again = new HttpRequestMessage(HttpMethod.Get, "/identity");
        again.Headers.TryAddWithoutValidation("X-Ip", "10.9.9.9");
        again.Headers.TryAddWithoutValidation("X-Sub", "user-1");
        Assert.Equal((HttpStatusCode)429, (await client.SendAsync(again, TestCt)).StatusCode);
    }

    [Fact]
    public async Task Invoke_WhitelistedNamedEndpoint_Returns200WithoutConsuming()
    {
        using var host = await StartHostAsync();
        var client = host.GetTestClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/named", TestCt)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/named", TestCt)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/named-sibling", TestCt)).StatusCode);
    }

    [Fact]
    public async Task Invoke_WhitelistedTemplate_SkipsConcretePath()
    {
        using var host = await StartHostAsync();
        var client = host.GetTestClient();
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/person/3", null, TestCt)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/person/3", null, TestCt)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/person-other", null, TestCt)).StatusCode);
    }

    [Fact]
    public async Task Invoke_EmptyWhitelist_OptedInRouteStillLimited()
    {
        using var host = await StartHostAsync();
        var client = host.GetTestClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/empty-list", TestCt)).StatusCode);
        Assert.Equal((HttpStatusCode)429, (await client.GetAsync("/empty-list", TestCt)).StatusCode);
    }

    private static async Task<IHost> StartHostAsync()
        => await new HostBuilder().ConfigureWebHost(web => web.UseTestServer()
                .ConfigureServices(services => {
                    services.AddLogging();
                    services.AddRouting();
                    services.AddLocalRateLimit();
                })
                .Configure(app => {
                    app.UseMiddleware<LoggingMiddleware>();
                    app.Use(async (HttpContext context, Func<Task> next) => {
                        var ip = context.Request.Headers.TryGetValue("X-Ip", out var rawIp) ? rawIp.ToString() : "10.0.0.8";
                        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse(ip);
                        if (context.Request.Headers.TryGetValue("X-Sub", out var sub) && !string.IsNullOrWhiteSpace(sub))
                            context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", sub.ToString())], "test"));

                        await next();
                    });
                    app.UseRouting();
                    app.UseEndpoints(endpoints => {
                        endpoints.MapGet("/under", () => Results.Ok()).RequireRateLimit("under", 4, TimeSpan.FromSeconds(10), RateLimitWindow.Fixed);
                        endpoints.MapGet("/over", () => Results.Ok()).RequireRateLimit("over", 4, TimeSpan.FromSeconds(10), RateLimitWindow.Fixed);
                        endpoints.MapGet("/open", () => Results.Ok());
                        endpoints.MapGet("/identity", () => Results.Ok()).RequireRateLimit("identity", 1, TimeSpan.FromSeconds(10), RateLimitWindow.Fixed);
                        endpoints.MapGet("/named", () => Results.Ok()).WithName("GetPerson")
                            .RequireRateLimit("named", 1, TimeSpan.FromSeconds(10), RateLimitWindow.Fixed, whitelist: ["GetPerson"]);
                        endpoints.MapGet("/named-sibling", () => Results.Ok())
                            .RequireRateLimit("named", 1, TimeSpan.FromSeconds(10), RateLimitWindow.Fixed);
                        endpoints.MapPost("/person/{id}", () => Results.Ok())
                            .RequireRateLimit("people", 1, TimeSpan.FromSeconds(10), RateLimitWindow.Fixed, whitelist: ["POST /person/{id}"]);
                        endpoints.MapPost("/person-other", () => Results.Ok())
                            .RequireRateLimit("people", 1, TimeSpan.FromSeconds(10), RateLimitWindow.Fixed);
                        endpoints.MapGet("/empty-list", () => Results.Ok())
                            .RequireRateLimit("empty", 1, TimeSpan.FromSeconds(10), RateLimitWindow.Fixed, whitelist: []);
                    });
                }))
            .StartAsync(TestCt);
}
