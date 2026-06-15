using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lyo.Authentication.AspNetCore;
using Lyo.Authentication.AspNetCore.Endpoints;
using Lyo.Authentication.OpenIdConnect.Client;
using Lyo.Authentication.OpenIdConnect.Coordinator;
using Lyo.Authentication.OpenIdConnect.Discovery;
using Lyo.Authentication.OpenIdConnect.Endpoints;
using Lyo.Authentication.OpenIdConnect.Pkce;
using Lyo.Authentication.OpenIdConnect.Provider;
using Lyo.Authentication.Options;
using Lyo.Common.Extensions;
using Lyo.Keystore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Lyo.Authentication.OpenIdConnect.Tests;

public sealed class EndToEndSmokeTests
{
    /// <summary>End-to-end smoke: BFF login → JWT-protected endpoint → mint PAT → call same endpoint with PAT.</summary>
    [Fact]
    public async Task FullFlow_Login_Jwt_MintPat_CallWithPat()
    {
        await using var fx = await SmokeFixture.CreateAsync();
        var (sealedState, nonce, state) = await fx.GenerateStateAsync();
        var code = fx.Idp.IssueCode(
            new Dictionary<string, object?> {
                ["sub"] = "smoke-user",
                ["iss"] = FakeOidcIdentityProvider.Issuer,
                ["aud"] = "test-client",
                ["email"] = "smoke@example.com",
                ["email_verified"] = true,
                ["name"] = "Smoke User",
                ["exp"] = DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds(),
                ["iat"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                ["nonce"] = nonce
            });

        var coordinator = fx.Services.GetRequiredService<IExternalLoginCoordinator>();
        var result = await coordinator.HandleCallbackAsync("fake", code, sealedState, state, TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.False(result.Issued.AccessToken.IsNullOrEmpty());
        using (var meRequest = new HttpRequestMessage(HttpMethod.Get, "/protected")) {
            meRequest.Headers.Authorization = new("Bearer", result.Issued.AccessToken);
            var resp = await fx.Client.SendAsync(meRequest, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
            var json = await resp.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
            Assert.False(json.GetProperty("sub").GetString().IsNullOrEmpty());
        }

        using (var createTokenRequest = new HttpRequestMessage(HttpMethod.Post, "/tokens")) {
            createTokenRequest.Headers.Authorization = new("Bearer", result.Issued.AccessToken);
            createTokenRequest.Content = JsonContent.Create(new { displayName = "smoke-pat", scopes = new[] { "people.read" } });
            var resp = await fx.Client.SendAsync(createTokenRequest, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
            var json = await resp.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
            var plaintext = json.GetProperty("plaintext").GetString();
            Assert.False(plaintext.IsNullOrEmpty());
            using var patRequest = new HttpRequestMessage(HttpMethod.Get, "/protected");
            patRequest.Headers.Authorization = new("Bearer", plaintext);
            var patResp = await fx.Client.SendAsync(patRequest, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, patResp.StatusCode);
        }
    }

    private sealed class SmokeFixture : IAsyncDisposable
    {
        public IHost Host = null!;
        public FakeOidcIdentityProvider Idp = null!;

        public IServiceProvider Services => Host.Services;

        public HttpClient Client => Host.GetTestClient();

        public async ValueTask DisposeAsync()
        {
            if (Host is not null) {
                await Host.StopAsync(TestContext.Current.CancellationToken);
                Host.Dispose();
            }
        }

        public async Task<(string sealedState, string nonce, string state)> GenerateStateAsync()
        {
            var coordinator = Services.GetRequiredService<IExternalLoginCoordinator>();
            var protector = Services.GetRequiredService<StateNonceProtector>();
            var redirect = await coordinator.BuildLoginRedirectAsync("fake", "/");
            var unsealed = protector.Unseal(redirect.SealedState) ?? throw new InvalidOperationException("could not unseal state");
            return (redirect.SealedState, unsealed.Nonce, unsealed.State);
        }

        public static async Task<SmokeFixture> CreateAsync()
        {
            var fx = new SmokeFixture { Idp = new() };
            var host = await new HostBuilder().ConfigureWebHost(webBuilder => {
                    webBuilder.UseTestServer()
                        .ConfigureServices(services => {
                            services.AddLogging();
                            services.AddRouting();
                            services.AddLocalKeyStore();
                            services.AddLyoAuthentication();
                            services.Configure<LyoJwtOptions>(o => {
                                o.Issuer = "https://lyo-smoke.test";
                                o.Audience = "lyo-smoke";
                            });

                            services.AddInMemoryAuthenticationStores();
                            services.AddLyoOpenIdConnect();
                            services.Configure<ExternalLoginOptions>(o => o.Policy = ExternalLoginPolicy.JustInTime);
                            services.AddSingleton<IOpenIdConnectProvider>(new ScopedFakeProvider("people.read", "auth.tokens.read", "auth.tokens.write"));
                            services.AddHttpClient<OidcDiscoveryCache>().ConfigurePrimaryHttpMessageHandler(() => fx.Idp.CreateHandler());
                            services.AddHttpClient<OidcJwksResolver>().ConfigurePrimaryHttpMessageHandler(() => fx.Idp.CreateHandler());
                            services.AddHttpClient<OidcTokenExchangeClient>().ConfigurePrimaryHttpMessageHandler(() => fx.Idp.CreateHandler());
                            services.AddLyoApiTokenAuthentication();
                            services.AddAuthorization();
                        })
                        .Configure(app => {
                            app.UseRouting();
                            app.UseAuthentication();
                            app.UseAuthorization();
                            app.UseEndpoints(endpoints => {
                                endpoints.MapGet(
                                        "/protected",
                                        (HttpContext ctx) => Results.Ok(new { sub = ctx.User.FindFirst("sub")?.Value ?? ctx.User.FindFirst("token_id")?.Value ?? string.Empty }))
                                    .RequireAuthorization();

                                endpoints.MapLyoJwks();
                                endpoints.MapLyoAuthEndpoints();
                                endpoints.MapLyoTokenManagementEndpoints();
                            });
                        });
                })
                .StartAsync(TestContext.Current.CancellationToken);

            fx.Host = host;
            return fx;
        }
    }

    private sealed class ScopedFakeProvider(params string[] scopes) : IOpenIdConnectProvider
    {
        public string Name => "fake";

        public string DiscoveryUrl => FakeOidcIdentityProvider.DiscoveryUrl;

        public string ClientId => "test-client";

        public string ClientSecret => "test-secret";

        public string RedirectUri => "https://localhost/callback";

        public IReadOnlyList<string> Scopes => new[] { "openid", "email", "profile" };

        public IReadOnlyDictionary<string, string> ExtraAuthorizeParameters => new Dictionary<string, string>();

        public OidcClaimMappingResult MapClaims(IReadOnlyDictionary<string, object?> claims)
        {
            var name = (claims.TryGetValue("name", out var n) ? n?.ToString() : null) ?? "unknown";
            var email = claims.TryGetValue("email", out var e) ? e?.ToString() : null;
            var verifiedRaw = claims.TryGetValue("email_verified", out var ev) ? ev : null;
            var verified = verifiedRaw is bool b ? b : true;
            return new(name, email, verified, null, null, scopes);
        }

        public string? PreflightReject(IReadOnlyDictionary<string, object?> claims) => null;
    }
}