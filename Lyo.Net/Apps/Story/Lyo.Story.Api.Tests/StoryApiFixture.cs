using System.Security.Claims;
using System.Text.Encodings.Web;
using Lyo.Common.Core.Identifiers;
using Lyo.Story.Api.Tests;
using Lyo.Story.Sqlite;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

[assembly: AssemblyFixture(typeof(StoryApiFixture))]

namespace Lyo.Story.Api.Tests;

/// <summary>
/// Test host serving <c>MapStoryRoutes</c> over a SQLite story store. The player ref is <c>player:{sub}</c> and the tenant is the <c>tenant_id</c> claim, as a Game server
/// binds them.
/// </summary>
public sealed class StoryApiFixture : IAsyncLifetime
{
    public const string AuthorPolicy = "story-author";
    public const string AuthorRole = "story-author";

    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"lyo-story-api-{LyoGuid.CreateV7():N}");
    private WebApplication _app = null!;
    private TestServer _server = null!;

    public async ValueTask InitializeAsync()
    {
        Directory.CreateDirectory(_directory);
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSqliteStoryStore(new SqliteStoryOptions { ConnectionString = $"Data Source={Path.Combine(_directory, "story.db")};Pooling=False" });
        builder.Services.AddAuthentication(StoryTestAuthHandler.Scheme).AddScheme<AuthenticationSchemeOptions, StoryTestAuthHandler>(StoryTestAuthHandler.Scheme, null);
        builder.Services.AddAuthorization(o => o.AddPolicy(AuthorPolicy, p => p.RequireAuthenticatedUser().RequireRole(AuthorRole)));
        _app = builder.Build();
        _app.UseAuthentication();
        _app.UseAuthorization();
        await _app.Services.MigrateSqliteStoryAsync();
        _app.MapGroup(string.Empty)
            .RequireAuthorization()
            .MapStoryRoutes(
                new() {
                    AuthorPolicy = AuthorPolicy,
                    PlayerRef = http => http.User.FindFirst("sub")?.Value is { } sub ? "player:" + sub : null,
                    TenantId = http => Guid.TryParse(http.User.FindFirst("tenant_id")?.Value, out var tenant) ? tenant : null
                });

        await _app.StartAsync();
        _server = _app.GetTestServer();
    }

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, true);
    }

    /// <summary>Client authenticated as <paramref name="subject" /> with optional comma-separated <paramref name="roles" /> and a <c>tenant_id</c> claim.</summary>
    public HttpClient CreateHttp(string subject, string? roles = null, Guid? tenant = null)
    {
        var http = _server.CreateClient();
        http.DefaultRequestHeaders.Add(StoryTestAuthHandler.SubjectHeader, subject);
        if (roles != null)
            http.DefaultRequestHeaders.Add(StoryTestAuthHandler.RolesHeader, roles);

        if (tenant != null)
            http.DefaultRequestHeaders.Add(StoryTestAuthHandler.TenantHeader, tenant.Value.ToString());

        return http;
    }
}

/// <summary>Authenticates from test headers: subject, comma-separated roles, and tenant.</summary>
internal sealed class StoryTestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string Scheme = "Test";
    public const string SubjectHeader = "X-Test-Sub";
    public const string RolesHeader = "X-Test-Roles";
    public const string TenantHeader = "X-Test-Tenant";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(SubjectHeader, out var subject) || string.IsNullOrWhiteSpace(subject))
            return Task.FromResult(AuthenticateResult.NoResult());

        var claims = new List<Claim> { new("sub", subject.ToString()) };
        if (Request.Headers.TryGetValue(TenantHeader, out var tenant))
            claims.Add(new("tenant_id", tenant.ToString()));

        if (Request.Headers.TryGetValue(RolesHeader, out var roles))
            claims.AddRange(roles.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries).Select(r => new Claim(ClaimTypes.Role, r.Trim())));

        return Task.FromResult(AuthenticateResult.Success(new(new(new ClaimsIdentity(claims, Scheme)), Scheme)));
    }
}