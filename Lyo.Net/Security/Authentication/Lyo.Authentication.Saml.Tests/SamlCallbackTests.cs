using System.Net;
using System.Net.Http.Json;
using Lyo.Api.Middleware;
using Lyo.Authentication;
using Lyo.Authentication.Sso;
using Lyo.Authentication.Sso.Endpoints;
using Lyo.KeyStore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Authentication.Saml.Tests;

public class SamlCallbackTests
{
    [Fact]
    public async Task HttpPost_ValidAssertion_MintsHandoff()
    {
        await using var host = await SamlCallbackHost.StartAsync();
        var location = await PostValidAsync(host.Client, signResponse: false);
        Assert.StartsWith("https://app.example/done?", location);
        Assert.Contains("lyo_handoff=", location);
    }

    [Fact]
    public async Task SamlPost_HandoffIssuedTo_IsConsumerOrigin()
    {
        await using var host = await SamlCallbackHost.StartAsync();
        var location = await PostValidAsync(host.Client, origin: "https://idp.example");
        var code = LocationQuery(location, "lyo_handoff");

        using var exchange = new HttpRequestMessage(HttpMethod.Post, "/auth/handoff/exchange");
        exchange.Headers.Add("Origin", "https://app.example");
        exchange.Content = JsonContent.Create(new { code });
        var response = await host.Client.SendAsync(exchange, TestCt);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>(TestCt);
        Assert.True(body!.ContainsKey("access_token"));
    }

    [Fact]
    public async Task SamlPost_MissingOidcCookie_StillCompletes()
    {
        await using var host = await SamlCallbackHost.StartAsync();
        var location = await PostValidAsync(host.Client);
        Assert.Contains("lyo_handoff=", location);
    }

    [Fact]
    public async Task SamlPost_BadSignature_NoHandoffRedirect()
    {
        await using var host = await SamlCallbackHost.StartAsync();
        var (relay, requestId) = await LoginAsync(host.Client);
        var spec = new SamlResponseSpec { RequestId = requestId, Now = DateTime.UtcNow, CorruptSignature = true, Signature = SamlSignatureKind.Response };
        var response = await PostAsync(host.Client, relay, spec.ToBase64(), origin: null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Null(response.Headers.Location);
    }

    [Fact]
    public async Task SamlPost_WithoutAntiforgeryToken_ReachesValidator()
    {
        await using var host = await SamlCallbackHost.StartAsync();
        var response = await PostValidResponseAsync(host.Client);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    [Fact]
    public async Task Callback_GetToSamlProvider_Rejects()
    {
        await using var host = await SamlCallbackHost.StartAsync();
        var response = await host.Client.GetAsync("/auth/callback/okta?code=abc&state=xyz", TestCt);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task<string> PostValidAsync(HttpClient client, bool signResponse = true, string? origin = null)
    {
        var response = await PostValidResponseAsync(client, signResponse, origin);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        return response.Headers.Location!.ToString();
    }

    private static async Task<HttpResponseMessage> PostValidResponseAsync(HttpClient client, bool signResponse = true, string? origin = null)
    {
        var (relay, requestId) = await LoginAsync(client);
        var spec = new SamlResponseSpec
        {
            RequestId = requestId,
            Now = DateTime.UtcNow,
            Signature = signResponse ? SamlSignatureKind.Response : SamlSignatureKind.Assertion
        };
        return await PostAsync(client, relay, spec.ToBase64(), origin);
    }

    private static async Task<(string Relay, string RequestId)> LoginAsync(HttpClient client)
    {
        var response = await client.GetAsync("/auth/login/okta?returnUrl=https://app.example/done", TestCt);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var (xml, relay) = SamlRedirect.Read(response.Headers.Location!.ToString());
        return (relay, SamlRedirect.RequestId(xml));
    }

    private static async Task<HttpResponseMessage> PostAsync(HttpClient client, string relay, string samlResponse, string? origin)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/auth/callback/okta");
        if (origin is not null)
            request.Headers.Add("Origin", origin);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string> { ["SAMLResponse"] = samlResponse, ["RelayState"] = relay });
        return await client.SendAsync(request, TestCt);
    }

    private static string LocationQuery(string location, string name)
    {
        var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(new Uri(location).Query);
        return query[name].ToString();
    }
}

internal sealed class SamlCallbackHost : IAsyncDisposable
{
    public required WebApplication App { get; init; }

    public HttpClient Client => App.GetTestClient();

    public static async Task<SamlCallbackHost> StartAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddLogging();
        builder.Services.AddLocalKeyStore();
        builder.Services.AddLyoAuthentication();
        builder.Services.AddInMemoryAuthenticationStores();
        builder.Services.AddSingleton<ISamlMetadataSource>(new StaticSamlMetadata(new SamlIdpMetadata(SamlTestProvider.Issuer, SamlTestProvider.SsoRedirect, [SamlTestCertificate.Signing.RawData])));
        builder.Services.AddSingleton<ISamlIdentityProvider>(SamlTestProvider.Default);
        builder.Services.AddLyoSaml();
        builder.Services.Configure<SsoBffOptions>(options => options.AllowedReturnOrigins.Add("https://app.example"));
        var app = builder.Build();
        app.UseMiddleware<LoggingMiddleware>();
        app.MapLyoAuthEndpoints();
        await app.StartAsync();
        return new SamlCallbackHost { App = app };
    }

    public async ValueTask DisposeAsync()
    {
        await App.StopAsync();
        await App.DisposeAsync();
    }
}
