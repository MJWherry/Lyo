using System;
using System.Net.Http;
using System.Threading.Tasks;
using Lyo.Authentication.AspNetCore.Authorization;
using Lyo.Authentication.AspNetCore.Endpoints;
using Lyo.Authentication.Format;
using Lyo.Authentication.Records;
using Lyo.Authentication.Services.Opaque;
using Lyo.Keystore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Lyo.Authentication.AspNetCore.Tests;

internal sealed class AuthenticationHandlerHarness : IAsyncDisposable
{
    private readonly IHost _host;

    private AuthenticationHandlerHarness(IHost host) => _host = host;

    public HttpClient Client => _host.GetTestClient();

    public IServiceProvider Services => _host.Services;

    public static async Task<AuthenticationHandlerHarness> CreateAsync(Action<IServiceCollection>? configureServices = null)
    {
        var host = await new HostBuilder()
            .ConfigureWebHost(webBuilder => {
                webBuilder
                    .UseTestServer()
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
                            endpoints.MapGet("/secure", (HttpContext ctx) => ctx.User.Identity?.Name ?? "no-name")
                                .RequireAuthorization();
                            endpoints.MapGet("/scoped", () => "scoped").RequireScope("people.read");
                            endpoints.MapLyoJwks();
                        });
                    });
            })
            .StartAsync();
        return new(host);
    }

    public async Task<string> IssueOpaqueAsync(params string[] scopes)
    {
        var issuer = _host.Services.GetRequiredService<IApiTokenIssuer>();
        var issued = await issuer.IssueAsync(new(
            Kind: ApiTokenKind.Pat,
            DisplayName: "test",
            Scopes: scopes,
            Ring: ApiTokenRing.Live));
        return issued.Plaintext;
    }

    public async ValueTask DisposeAsync()
    {
        await _host.StopAsync();
        _host.Dispose();
    }
}
