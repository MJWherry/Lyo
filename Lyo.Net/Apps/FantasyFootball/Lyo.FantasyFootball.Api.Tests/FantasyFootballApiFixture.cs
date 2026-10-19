using System.Net;
using System.Text;
using Lyo.Espn.Fantasy.Football.Client;
using Lyo.FantasyFootball.Postgres;
using Lyo.FantasyFootball.Postgres.Database;
using Lyo.Testing.Containers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.FantasyFootball.Api.Tests;

public sealed class FantasyFootballApiFixture : PostgresServiceFixtureBase<FantasyFootballDbContext>
{
    protected override LogLevel MinimumLogLevel => LogLevel.Warning;

    protected override void ConfigureServices(IServiceCollection services, string connectionString)
        => services.AddPostgresFantasyFootballStore(new PostgresFantasyFootballOptions { ConnectionString = connectionString, EnableAutoMigrations = true });
}

internal sealed class RecordingHandler : HttpMessageHandler
{
    public List<CapturedRequest> Calls { get; } = [];

    public Func<CapturedRequest, HttpResponseMessage> Respond { get; set; } = _ => new(HttpStatusCode.OK);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var cookie = request.Headers.TryGetValues("Cookie", out var values) ? string.Join("; ", values) : null;
        var captured = new CapturedRequest(request.Method, request.RequestUri!, body, cookie);
        Calls.Add(captured);
        return Respond(captured);
    }
}

internal sealed record CapturedRequest(HttpMethod Method, Uri Uri, string? Body, string? Cookie);
