using System.Net;
using Lyo.Authentication.Services.Jwt;
using Lyo.Authentication.Services.Users;

namespace Lyo.Authentication.AspNetCore.Tests;

public sealed class LyoJwtAuthenticationTests
{
    private CancellationToken TCT => TestContext.Current.CancellationToken;

    [Fact]
    public async Task SecureEndpoint_AcceptsValidLyoJwt()
    {
        await using var harness = await AuthenticationHandlerHarness.CreateAsync();
        var jwt = await IssueJwtAsync(harness, "people.read");
        var request = new HttpRequestMessage(HttpMethod.Get, "/secure");
        request.Headers.Authorization = new("Bearer", jwt);
        var response = await harness.Client.SendAsync(request, TCT);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ScopedEndpoint_AcceptsJwtWithMatchingScope()
    {
        await using var harness = await AuthenticationHandlerHarness.CreateAsync();
        var jwt = await IssueJwtAsync(harness, "people.read");
        var request = new HttpRequestMessage(HttpMethod.Get, "/scoped");
        request.Headers.Authorization = new("Bearer", jwt);
        var response = await harness.Client.SendAsync(request, TCT);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ScopedEndpoint_RejectsJwtWithoutScope()
    {
        await using var harness = await AuthenticationHandlerHarness.CreateAsync();
        var jwt = await IssueJwtAsync(harness, "orders.read");
        var request = new HttpRequestMessage(HttpMethod.Get, "/scoped");
        request.Headers.Authorization = new("Bearer", jwt);
        var response = await harness.Client.SendAsync(request, TCT);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SecureEndpoint_RejectsGarbageJwt()
    {
        await using var harness = await AuthenticationHandlerHarness.CreateAsync();
        var request = new HttpRequestMessage(HttpMethod.Get, "/secure");
        request.Headers.Authorization = new("Bearer", "eyJhbGciOiJub25lIn0.e30.");
        var response = await harness.Client.SendAsync(request, TCT);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<string> IssueJwtAsync(AuthenticationHandlerHarness harness, params string[] scopes)
    {
        var userStore = harness.Services.GetRequiredService<IUserStore>();
        var user = await userStore.CreateAsync(
            new(Guid.NewGuid(), "Jwt User", $"jwt-{Guid.NewGuid():N}@example.com", true, null, null, scopes, null, null, DateTime.UtcNow, null, null, null, null), null, TCT);

        var issuer = harness.Services.GetRequiredService<ILyoJwtIssuer>();
        var issued = await issuer.IssueAsync(user, scopes, "local", null, false, TCT);
        return issued.AccessToken;
    }
}
