using System.Net;
using Lyo.Api.Middleware;
using Lyo.Authentication;
using Lyo.Authentication.Sso.Endpoints;
using Lyo.Authentication.Sso.Protocol;
using Lyo.KeyStore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Authentication.Sso.Tests;

public sealed class SsoLoginEndpointTests
{
    [Fact]
    public async Task Login_UnknownProvider_Returns400()
    {
        await using var app = await StartAsync(services => services.AddLyoSso());
        var client = app.GetTestClient();
        var response = await client.GetAsync("/auth/login/missing?returnUrl=/home", TestCt);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_OffListReturnUrl_Returns400AndDoesNotStartSaml()
    {
        var protocol = new FlagProtocol();
        await using var app = await StartAsync(services => {
            services.AddLyoSso();
            services.AddSingleton(protocol);
            services.AddSingleton<ISsoProtocol>(protocol);
        });

        var client = app.GetTestClient();
        var response = await client.GetAsync("/auth/login/widget?returnUrl=https://evil.example/steal", TestCt);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(protocol.Started);
    }

    [Fact]
    public async Task Login_ForwardedPrefix_CookiePathIncludesPrefix()
    {
        var protocol = new FlagProtocol();
        await using var app = await StartAsync(services => {
            services.AddLyoSso();
            services.AddSingleton(protocol);
            services.AddSingleton<ISsoProtocol>(protocol);
        });

        var client = app.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/auth/login/widget?returnUrl=/home");
        request.Headers.TryAddWithoutValidation("X-Forwarded-Prefix", "/api");
        var response = await client.SendAsync(request, TestCt);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookies));
        var cookie = string.Join(";", cookies);
        Assert.Contains("lyo_oidc_state=", cookie, StringComparison.Ordinal);
        Assert.Contains("path=/api/auth/callback", cookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Callback_PostToOidcProvider_Rejects()
    {
        await using var app = await StartAsync(services => {
            services.AddLyoSso();
            services.AddSingleton<ISsoProtocol>(new FlagProtocol());
        });

        var client = app.GetTestClient();
        var response = await client.PostAsync("/auth/callback/widget", new StringContent(""), TestCt);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task<WebApplication> StartAsync(Action<IServiceCollection> configure)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development", Args = [] });
        builder.WebHost.UseTestServer();
        builder.Services.AddLogging();
        builder.Services.AddLocalKeyStore();
        builder.Services.AddLyoAuthentication();
        builder.Services.AddInMemoryAuthenticationStores();
        configure(builder.Services);
        var app = builder.Build();
        app.UseMiddleware<LoggingMiddleware>();
        app.MapLyoAuthEndpoints();
        await app.StartAsync(TestCt);
        return app;
    }

    private sealed class FlagProtocol : ISsoProtocol
    {
        public bool Started { get; private set; }

        public string Name => "oidc";

        public SsoCallbackMethod CallbackMethod => SsoCallbackMethod.Get;

        public IReadOnlyCollection<string> ProviderNames => ["widget"];

        public Task<SsoLoginStart> StartAsync(string providerName, string returnUrl, string mode, CancellationToken ct = default)
        {
            Started = true;
            return Task.FromResult(new SsoLoginStart("https://idp.example/authorize", "sealed-state"));
        }

        public Task<ExternalLoginResult> CompleteAsync(SsoCallback callback, CancellationToken ct = default) => throw new NotSupportedException();
    }
}
