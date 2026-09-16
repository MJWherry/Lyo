using System.Net;
using Lyo.Authentication.Services.Jwt;
using Lyo.Authentication.Services.Users;
using Lyo.Common.Core.Identifiers;

namespace Lyo.Authentication.AspNetCore.Tests;

public sealed class LyoJwtAuthenticationTests
{
    [Fact]
    public async Task SecureEndpoint_AcceptsValidLyoJwt()
    {
        await using var harness = await AuthenticationHandlerHarness.CreateAsync();
        var jwt = await IssueJwtAsync(harness, "people.read");
        var request = new HttpRequestMessage(HttpMethod.Get, "/secure");
        request.Headers.Authorization = new("Bearer", jwt);
        var response = await harness.Client.SendAsync(request, TestCt);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ScopedEndpoint_AcceptsJwtWithMatchingScope()
    {
        await using var harness = await AuthenticationHandlerHarness.CreateAsync();
        var jwt = await IssueJwtAsync(harness, "people.read");
        var request = new HttpRequestMessage(HttpMethod.Get, "/scoped");
        request.Headers.Authorization = new("Bearer", jwt);
        var response = await harness.Client.SendAsync(request, TestCt);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ScopedEndpoint_RejectsJwtWithoutScope()
    {
        await using var harness = await AuthenticationHandlerHarness.CreateAsync();
        var jwt = await IssueJwtAsync(harness, "orders.read");
        var request = new HttpRequestMessage(HttpMethod.Get, "/scoped");
        request.Headers.Authorization = new("Bearer", jwt);
        var response = await harness.Client.SendAsync(request, TestCt);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SecureEndpoint_RejectsGarbageJwt()
    {
        await using var harness = await AuthenticationHandlerHarness.CreateAsync();
        var request = new HttpRequestMessage(HttpMethod.Get, "/secure");
        request.Headers.Authorization = new("Bearer", "eyJhbGciOiJub25lIn0.e30.");
        var response = await harness.Client.SendAsync(request, TestCt);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<string> IssueJwtAsync(AuthenticationHandlerHarness harness, params string[] scopes)
    {
        var userStore = harness.Services.GetRequiredService<IUserStore>();
        var user = await userStore.CreateAsync(
            new(LyoGuid.CreateV7(), "Jwt User", $"jwt-{LyoGuid.CreateV7():N}@example.com", true, null, null, scopes, null, null, DateTime.UtcNow, null, null, null, null), null,
            TestCt);

        var issuer = harness.Services.GetRequiredService<ILyoJwtIssuer>();
        var issued = await issuer.IssueAsync(user, scopes, "local", null, false, TestCt);
        return issued.AccessToken;
    }
}