using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Lyo.Authentication.Models.Records;
using Lyo.Authentication.Services.Jwt;
using Lyo.Authentication.Services.Users;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Authentication.AspNetCore.Tests;

public sealed class LyoJwtAuthenticationTests
{
    [Fact]
    public async Task SecureEndpoint_AcceptsValidLyoJwt()
    {
        await using var harness = await AuthenticationHandlerHarness.CreateAsync();
        var jwt = await IssueJwtAsync(harness, "people.read");
        var request = new HttpRequestMessage(HttpMethod.Get, "/secure");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        var response = await harness.Client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ScopedEndpoint_AcceptsJwtWithMatchingScope()
    {
        await using var harness = await AuthenticationHandlerHarness.CreateAsync();
        var jwt = await IssueJwtAsync(harness, "people.read");
        var request = new HttpRequestMessage(HttpMethod.Get, "/scoped");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        var response = await harness.Client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ScopedEndpoint_RejectsJwtWithoutScope()
    {
        await using var harness = await AuthenticationHandlerHarness.CreateAsync();
        var jwt = await IssueJwtAsync(harness, "orders.read");
        var request = new HttpRequestMessage(HttpMethod.Get, "/scoped");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        var response = await harness.Client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SecureEndpoint_RejectsGarbageJwt()
    {
        await using var harness = await AuthenticationHandlerHarness.CreateAsync();
        var request = new HttpRequestMessage(HttpMethod.Get, "/secure");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "eyJhbGciOiJub25lIn0.e30.");
        var response = await harness.Client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static async Task<string> IssueJwtAsync(AuthenticationHandlerHarness harness, params string[] scopes)
    {
        var userStore = harness.Services.GetRequiredService<IUserStore>();
        var user = await userStore.CreateAsync(new LyoUser(
            Id: Guid.NewGuid(),
            DisplayName: "Jwt User",
            Email: $"jwt-{Guid.NewGuid():N}@example.com",
            EmailVerified: true,
            AvatarUrl: null,
            PreferredLanguageBcp47: null,
            Scopes: scopes,
            Metadata: null,
            PersonId: null,
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: null,
            LastLoginAt: null,
            DisabledAt: null,
            DisabledReason: null), tenantId: null);
        var issuer = harness.Services.GetRequiredService<ILyoJwtIssuer>();
        var issued = await issuer.IssueAsync(user, scopes, provider: "local", externalSubject: null, includeRefresh: false);
        return issued.AccessToken;
    }
}
