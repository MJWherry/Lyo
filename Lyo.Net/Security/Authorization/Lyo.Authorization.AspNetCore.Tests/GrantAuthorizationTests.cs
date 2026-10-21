using System.Security.Claims;
using System.Text.Encodings.Web;
using Lyo.Authentication;
using Lyo.Authentication.AspNetCore;
using Lyo.Authentication.AspNetCore.Authorization;
using Lyo.KeyStore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lyo.Authorization.AspNetCore.Tests;

public sealed class GrantAuthorizationTests
{
    private static readonly Guid NoteId = Guid.Parse("11111111-2222-3333-4444-555555555555");

    [Fact]
    public async Task AuthThenGrant_ForwardsScopePolicies_AndSecondRegistrationIsANoOp()
    {
        var services = new ServiceCollection();
        services.AddAuthorization();
        services.AddLyoAuthentication();
        services.AddLyoApiTokenAuthentication();
        services.AddLyoGrantAuthorization();
        services.AddLyoGrantAuthorization();
        using var provider = services.BuildServiceProvider();
        var policies = provider.GetRequiredService<IAuthorizationPolicyProvider>();
        var scope = await policies.GetPolicyAsync("scope:people.read");
        Assert.NotNull(scope);
        Assert.Contains(scope.Requirements, r => r is ScopeAuthorizationRequirement);
        var fallback = await policies.GetFallbackPolicyAsync();
        Assert.Null(fallback);
        var grant = await policies.GetPolicyAsync(GrantPolicyNames.For("read", "note", "id"));
        Assert.Contains(grant!.Requirements, r => r is GrantAuthorizationRequirement);
    }

    [Fact]
    public async Task MinimalApi_Anonymous401_MissingGrant403_Both200_LyoUserBeatsSub_BadTenant403_Store500()
    {
        await using var host = await HostAsync(services => services.AddInMemoryAuthorization(() => DateTime.UtcNow));
        var store = host.Services.GetRequiredService<IGrantStore>();
        await store.SaveAsync(
            new() {
                Subject = "user-a",
                Action = "read",
                ResourceType = "note",
                ResourceId = NoteId.ToString("D")
            }, TestCt);

        Assert.Equal(StatusCodes.Status401Unauthorized, (await SendAsync(host, "/notes/" + NoteId)).Status);
        Assert.Equal(StatusCodes.Status403Forbidden, (await SendAsync(host, "/notes/" + NoteId, "note.read", "user-a")).Status);
        Assert.Equal(StatusCodes.Status200OK, (await SendAsync(host, "/notes/" + NoteId, "note.read", "user-a", role: "editor")).Status);
        await store.SaveAsync(
            new() {
                Subject = "sub-only",
                Action = "read",
                ResourceType = "note",
                ResourceId = NoteId.ToString("D")
            }, TestCt);

        Assert.Equal(StatusCodes.Status403Forbidden, (await SendAsync(host, "/notes/" + NoteId, "note.read", "sub-only", "other", "editor")).Status);
        Assert.Equal(StatusCodes.Status200OK, (await SendAsync(host, "/notes/" + NoteId, "note.read", "ignored", "user-a", "editor")).Status);
        Assert.Equal(StatusCodes.Status403Forbidden, (await SendAsync(host, "/notes/" + NoteId + "/tenants/not-a-guid", "note.read", "user-a", role: "editor")).Status);
        await using var broken = await HostAsync(services => services.AddSingleton<IResourceAuthorization, ThrowingAuthorization>());
        Assert.Equal(StatusCodes.Status500InternalServerError, (await SendAsync(broken, "/notes/" + NoteId, "note.read", "user-a", role: "editor")).Status);
    }

    [Fact]
    public async Task Controller_RequiresScopeRoleAndGrant_AllowAnonymousSkipsClassScope_ClassGrantReadsRoute()
    {
        await using var host = await HostAsync(
            services => {
                services.AddInMemoryAuthorization(() => DateTime.UtcNow);
                services.AddControllers().AddApplicationPart(typeof(NoteController).Assembly);
            }, app => app.MapControllers());

        var store = host.Services.GetRequiredService<IGrantStore>();
        await store.SaveAsync(
            new() {
                Subject = "user-a",
                Action = "read",
                ResourceType = "note",
                ResourceId = NoteId.ToString("D")
            }, TestCt);

        Assert.Equal(StatusCodes.Status403Forbidden, (await SendAsync(host, "/ctrl/notes/" + NoteId, "note.read", "user-a")).Status);
        Assert.Equal(StatusCodes.Status403Forbidden, (await SendAsync(host, "/ctrl/notes/" + NoteId, "note.read", "user-a", role: "reader")).Status);
        Assert.Equal(StatusCodes.Status200OK, (await SendAsync(host, "/ctrl/notes/" + NoteId, "note.read", "user-a", role: "editor")).Status);
        Assert.Equal(StatusCodes.Status200OK, (await SendAsync(host, "/ctrl/roles/" + NoteId, "note.read", "user-a", role: "editor")).Status);
        Assert.Equal(StatusCodes.Status200OK, (await SendAsync(host, "/ctrl/open")).Status);
        Assert.Equal(StatusCodes.Status200OK, (await SendAsync(host, "/class/notes/" + NoteId, sub: "user-a")).Status);
    }

    private static async Task<WebApplication> HostAsync(Action<IServiceCollection> configure, Action<WebApplication>? map = null)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddAuthorization();
        builder.Services.AddLocalKeyStore();
        builder.Services.AddLyoAuthentication();
        builder.Services.AddInMemoryAuthenticationStores();
        builder.Services.AddLyoApiTokenAuthentication();
        builder.Services.AddAuthentication().AddScheme<AuthenticationSchemeOptions, HeaderAuthHandler>("test", _ => { });
        builder.Services.PostConfigure<AuthenticationOptions>(o => {
            o.DefaultAuthenticateScheme = "test";
            o.DefaultChallengeScheme = "test";
        });

        builder.Services.AddLyoGrantAuthorization();
        configure(builder.Services);
        var app = builder.Build();
        app.UseExceptionHandler(
            new ExceptionHandlerOptions {
                ExceptionHandler = context => {
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    return Task.CompletedTask;
                }
            });

        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        if (map is null) {
            app.MapGet("/notes/{id:guid}", () => Results.Ok("note")).RequireScope("note.read").RequireRole("editor").RequireGrant("read", "note", "id");
            app.MapGet("/notes/{id:guid}/tenants/{tenantId}", () => Results.Ok("tenant"))
                .RequireScope("note.read")
                .RequireRole("editor")
                .RequireGrant("read", "note", "id", "tenantId");
        }
        else
            map(app);

        await app.StartAsync(TestCt);
        return app;
    }

    private static async Task<Captured> SendAsync(WebApplication app, string path, string? scope = null, string? sub = null, string? lyoUser = null, string? role = null)
    {
        var client = app.GetTestClient();
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (scope is not null)
            request.Headers.TryAddWithoutValidation("X-Test-Scope", scope);

        if (sub is not null)
            request.Headers.TryAddWithoutValidation("X-Test-Sub", sub);

        if (lyoUser is not null)
            request.Headers.TryAddWithoutValidation("X-Test-Lyo-User", lyoUser);

        if (role is not null)
            request.Headers.TryAddWithoutValidation("X-Test-Role", role);

        var response = await client.SendAsync(request, TestCt);
        return new((int)response.StatusCode);
    }

    private sealed record Captured(int Status);

    private sealed class ThrowingAuthorization : IResourceAuthorization
    {
        public Task<Grant> SaveAsync(GrantWrite write, CancellationToken ct = default) => throw new InvalidOperationException("store down");

        public Task<Grant> RevokeAsync(Guid id, CancellationToken ct = default) => throw new InvalidOperationException("store down");

        public Task<bool> IsAllowedAsync(GrantCheck check, CancellationToken ct = default) => throw new InvalidOperationException("store down");
    }
}

public sealed class HeaderAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public HeaderAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey("X-Test-Sub") && !Request.Headers.ContainsKey("X-Test-Lyo-User") && !Request.Headers.ContainsKey("X-Test-Scope"))
            return Task.FromResult(AuthenticateResult.NoResult());

        var claims = new List<Claim>();
        Add(claims, "sub", "X-Test-Sub");
        Add(claims, "lyo:user", "X-Test-Lyo-User");
        Add(claims, "scope", "X-Test-Scope");
        if (Request.Headers.TryGetValue("X-Test-Role", out var role) && !string.IsNullOrWhiteSpace(role))
            claims.Add(new(ClaimTypes.Role, role.ToString()));

        var identity = new ClaimsIdentity(claims, "test");
        return Task.FromResult(AuthenticateResult.Success(new(new(identity), Scheme.Name)));
    }

    private void Add(List<Claim> claims, string type, string header)
    {
        if (Request.Headers.TryGetValue(header, out var value) && !string.IsNullOrWhiteSpace(value))
            claims.Add(new(type, value.ToString()));
    }
}

[ApiController]
[RequireScope("note.read")]
public sealed class NoteController : ControllerBase
{
    [HttpGet("/ctrl/notes/{id:guid}")]
    [RequireRole("editor")]
    [RequireGrant("read", "note", "id")]
    public IActionResult Get() => Ok("note");

    [HttpGet("/ctrl/roles/{id:guid}")]
    [Authorize(Roles = "editor")]
    public IActionResult Role() => Ok("role");

    [HttpGet("/ctrl/open")]
    [AllowAnonymous]
    public IActionResult Open() => Ok("open");
}

[ApiController]
[RequireGrant("read", "note", "id")]
public sealed class ClassGrantController : ControllerBase
{
    [HttpGet("/class/notes/{id:guid}")]
    public IActionResult Get() => Ok("class");
}