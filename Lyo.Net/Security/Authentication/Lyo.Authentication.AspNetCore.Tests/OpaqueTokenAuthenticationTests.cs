using System.Net;
using Lyo.Authentication.Scopes;

namespace Lyo.Authentication.AspNetCore.Tests;

public sealed class OpaqueTokenAuthenticationTests
{
    [Fact]
    public async Task SecureEndpoint_RejectsAnonymous()
    {
        await using var harness = await AuthenticationHandlerHarness.CreateAsync();
        var response = await harness.Client.GetAsync("/secure");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SecureEndpoint_AcceptsValidOpaqueToken()
    {
        await using var harness = await AuthenticationHandlerHarness.CreateAsync();
        var token = await harness.IssueOpaqueAsync("people.read");
        var request = new HttpRequestMessage(HttpMethod.Get, "/secure");
        request.Headers.Authorization = new("Bearer", token);
        var response = await harness.Client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SecureEndpoint_RejectsInvalidToken()
    {
        await using var harness = await AuthenticationHandlerHarness.CreateAsync();
        var request = new HttpRequestMessage(HttpMethod.Get, "/secure");
        request.Headers.Authorization = new("Bearer", "lyo_pat_live_AAAAAAAAAAA_AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
        var response = await harness.Client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SecureEndpoint_AcceptsTokenViaXApiKeyHeader()
    {
        await using var harness = await AuthenticationHandlerHarness.CreateAsync();
        var token = await harness.IssueOpaqueAsync();
        var request = new HttpRequestMessage(HttpMethod.Get, "/secure");
        request.Headers.Add("X-Api-Key", token);
        var response = await harness.Client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ScopedEndpoint_RejectsTokenWithoutScope()
    {
        await using var harness = await AuthenticationHandlerHarness.CreateAsync();
        var token = await harness.IssueOpaqueAsync("orders.read");
        var request = new HttpRequestMessage(HttpMethod.Get, "/scoped");
        request.Headers.Authorization = new("Bearer", token);
        var response = await harness.Client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ScopedEndpoint_AcceptsTokenWithScope()
    {
        await using var harness = await AuthenticationHandlerHarness.CreateAsync();
        var token = await harness.IssueOpaqueAsync("people.read");
        var request = new HttpRequestMessage(HttpMethod.Get, "/scoped");
        request.Headers.Authorization = new("Bearer", token);
        var response = await harness.Client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AnonymousEndpoint_AllowsMissingCredential()
    {
        await using var harness = await AuthenticationHandlerHarness.CreateAsync();
        var response = await harness.Client.GetAsync("/anon");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ScopedEndpoint_AcceptsTransitivelyImpliedScope()
    {
        await using var harness = await AuthenticationHandlerHarness.CreateAsync(services => {
            services.AddScope("people.read", "Read people");
            services.AddScope("admin", "Full admin", "people.read");
        });

        var token = await harness.IssueOpaqueAsync("admin");
        var request = new HttpRequestMessage(HttpMethod.Get, "/scoped");
        request.Headers.Authorization = new("Bearer", token);
        var response = await harness.Client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}