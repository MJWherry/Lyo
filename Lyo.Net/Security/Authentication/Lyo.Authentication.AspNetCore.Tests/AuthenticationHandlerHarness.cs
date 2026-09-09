using Lyo.Authentication.AspNetCore.Authorization;
using Lyo.Authentication.AspNetCore.Endpoints;
using Lyo.Authentication.Models.Format;
using Lyo.Authentication.Services.Opaque;
using Lyo.KeyStore;
using Microsoft.AspNetCore.TestHost;

namespace Lyo.Authentication.AspNetCore.Tests;

internal sealed class AuthenticationHandlerHarness : IAsyncDisposable
{
    private readonly IHost _host;

    public HttpClient Client => _host.GetTestClient();

    public IServiceProvider Services => _host.Services;

    private AuthenticationHandlerHarness(IHost host) => _host = host;

    public async ValueTask DisposeAsync()
    {
        await _host.StopAsync(TestContext.Current.CancellationToken);
        _host.Dispose();
    }

    public static async Task<AuthenticationHandlerHarness> CreateAsync(Action<IServiceCollection>? configureServices = null)
    {
        var host = await new HostBuilder().ConfigureWebHost(webBuilder => {
                webBuilder.UseTestServer()
                    .ConfigureServices(services => {
                        services.AddLocalKeyStore();
                        services.AddLyoAuthentication();
                        services.AddInMemoryAuthenticationStores();
                        services.AddLyoApiTokenAuthentication();
                        services.AddAuthorization();
                        services.AddRouting();
                        configureServices?.Invoke(services);
                    })
                    .Configure(app => {
                        app.UseRouting();
                        app.UseAuthentication();
                        app.UseAuthorization();
                        app.UseEndpoints(endpoints => {
                            endpoints.MapGet("/anon", () => "ok").AllowAnonymous();
                            endpoints.MapGet("/secure", (HttpContext ctx) => ctx.User.Identity?.Name ?? "no-name").RequireAuthorization();
                            endpoints.MapGet("/scoped", () => "scoped").RequireScope("people.read");
                            endpoints.MapLyoJwks();
                        });
                    });
            })
            .StartAsync(TestContext.Current.CancellationToken);

        return new(host);
    }

    public async Task<string> IssueOpaqueAsync(params string[] scopes)
    {
        var issuer = _host.Services.GetRequiredService<IApiTokenIssuer>();
        var issued = await issuer.IssueAsync(new(ApiTokenKind.Pat, "test", scopes, Ring: ApiTokenRing.Live), TestContext.Current.CancellationToken);
        return issued.Plaintext;
    }
}