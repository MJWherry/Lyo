using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Authorization.AspNetCore.Tests;

public sealed class GrantEndpointTests
{
    [Fact]
    public async Task List_Anonymous401_SelfOnlyWithoutManage_ManageCanSaveRevokeAndCheck()
    {
        await using var host = await HostAsync();
        var store = host.Services.GetRequiredService<IGrantStore>();
        await store.SaveAsync(new() { Subject = "user-a", Action = "read", ResourceType = "note" }, TestCt);
        await store.SaveAsync(new() { Subject = "user-b", Action = "read", ResourceType = "note" }, TestCt);
        var client = host.GetTestClient();
        Assert.Equal(StatusCodes.Status401Unauthorized, (int)(await client.GetAsync("/authorization/grants", TestCt)).StatusCode);
        var self = await client.SendAsync(Get("/authorization/grants", "user-a"), TestCt);
        var selfBody = await self.Content.ReadAsStringAsync(TestCt);
        Assert.Equal(StatusCodes.Status200OK, (int)self.StatusCode);
        Assert.Contains("user-a", selfBody, StringComparison.Ordinal);
        Assert.DoesNotContain("user-b", selfBody, StringComparison.Ordinal);
        var other = await client.SendAsync(Get("/authorization/grants?subject=user-b", "user-a"), TestCt);
        Assert.Equal(StatusCodes.Status403Forbidden, (int)other.StatusCode);
        var managed = await client.SendAsync(Get("/authorization/grants?subject=user-b", "user-a", true), TestCt);
        Assert.Equal(StatusCodes.Status200OK, (int)managed.StatusCode);
        Assert.Contains("user-b", await managed.Content.ReadAsStringAsync(TestCt), StringComparison.Ordinal);
        var deniedSave = await client.SendAsync(Post("/authorization/grants", "user-a", new GrantWrite { Subject = "user-c", Action = "read", ResourceType = "note" }), TestCt);
        Assert.Equal(StatusCodes.Status403Forbidden, (int)deniedSave.StatusCode);
        var saved = await client.SendAsync(Post("/authorization/grants", "user-a", new GrantWrite { Subject = "user-c", Action = "read", ResourceType = "note" }, true), TestCt);
        Assert.Equal(StatusCodes.Status200OK, (int)saved.StatusCode);
        var check = await client.SendAsync(Post("/authorization/grants/check", "user-a", new GrantCheck { Subject = "user-a", Action = "read", ResourceType = "note" }), TestCt);
        Assert.Equal(StatusCodes.Status200OK, (int)check.StatusCode);
        Assert.Contains("true", await check.Content.ReadAsStringAsync(TestCt), StringComparison.OrdinalIgnoreCase);
        var otherCheck = await client.SendAsync(
            Post("/authorization/grants/check", "user-a", new GrantCheck { Subject = "user-b", Action = "read", ResourceType = "note" }), TestCt);

        Assert.Equal(StatusCodes.Status403Forbidden, (int)otherCheck.StatusCode);
    }

    private static async Task<WebApplication> HostAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddAuthentication().AddScheme<AuthenticationSchemeOptions, HeaderAuthHandler>("test", _ => { });
        builder.Services.PostConfigure<AuthenticationOptions>(o => {
            o.DefaultAuthenticateScheme = "test";
            o.DefaultChallengeScheme = "test";
        });

        builder.Services.AddAuthorization(o => o.AddPolicy(GrantEndpoints.ManagePolicyName, p => p.RequireRole("grant-admin")));
        builder.Services.AddInMemoryAuthorization();
        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapGrantEndpoints(GrantEndpoints.ManagePolicyName);
        await app.StartAsync(TestCt);
        return app;
    }

    private static HttpRequestMessage Get(string path, string subject, bool manage = false)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        Stamp(request, subject, manage);
        return request;
    }

    private static HttpRequestMessage Post<T>(string path, string subject, T body, bool manage = false)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(body) };
        Stamp(request, subject, manage);
        return request;
    }

    private static void Stamp(HttpRequestMessage request, string subject, bool manage)
    {
        request.Headers.Add("X-Test-Sub", subject);
        if (manage)
            request.Headers.Add("X-Test-Role", "grant-admin");
    }
}