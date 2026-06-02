using System.Net;
using System.Text.Json;

namespace Lyo.Authentication.AspNetCore.Tests;

public sealed class JwksEndpointTests
{
    private CancellationToken TCT => TestContext.Current.CancellationToken;

    [Fact]
    public async Task JwksEndpoint_ReturnsKeys()
    {
        await using var harness = await AuthenticationHandlerHarness.CreateAsync();
        await harness.IssueOpaqueAsync();
        var response = await harness.Client.GetAsync("/.well-known/jwks.json", TCT);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(TCT);
        using var doc = JsonDocument.Parse(body);
        var keys = doc.RootElement.GetProperty("keys");
        Assert.Equal(JsonValueKind.Array, keys.ValueKind);
        Assert.True(keys.GetArrayLength() >= 1);
        var first = keys[0];
        Assert.Equal("OKP", first.GetProperty("kty").GetString());
        Assert.Equal("Ed25519", first.GetProperty("crv").GetString());
    }
}