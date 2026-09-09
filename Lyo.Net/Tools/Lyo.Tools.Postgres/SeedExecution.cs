using Lyo.Api.Client;
using Lyo.Comic.Postgres.Database;
using Lyo.People.Postgres.Database;
using Lyo.Seed;
using Lyo.Tools.Postgres.Seeds;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Tools.Postgres;

/// <summary>Constructs EF/API transports and executes a <see cref="SeedContributor"/>.</summary>
internal static class SeedExecution
{
    public const string DefaultApiBaseUrl = "http://localhost:5251/";

    public static async Task<SeedResult> RunAsync(
        SeedContributor contributor,
        ISeedRunner runner,
        ConnectionStringProvider connStr,
        SeedTransportKind via,
        SeedOptions options,
        string? apiBaseUrl,
        CancellationToken ct)
    {
        if ((contributor.SupportedTransports & via) == 0)
            throw new SeedException($"{contributor.Name} does not support {via}.");

        if (via == SeedTransportKind.Api)
            return await RunApiAsync(contributor, runner, options, apiBaseUrl, ct).ConfigureAwait(false);

        return contributor switch {
            PeopleEfSeedContributor => await RunEfAsync<PeopleDbContext>(contributor, runner, connStr, "people", options, ct).ConfigureAwait(false),
            ComicEfSeedContributor => await RunEfAsync<ComicDbContext>(contributor, runner, connStr, "comic", options, ct).ConfigureAwait(false),
            var _ => throw new SeedException($"No EF context mapping for {contributor.Name}.")
        };
    }

    private static async Task<SeedResult> RunEfAsync<TContext>(
        SeedContributor contributor,
        ISeedRunner runner,
        ConnectionStringProvider connStr,
        string schema,
        SeedOptions options,
        CancellationToken ct)
        where TContext : DbContext
    {
        var connection = connStr.GetOrThrow();
        var dbOptions = new DbContextOptionsBuilder<TContext>().UseNpgsql(connection, n => n.MigrationsHistoryTable("__EFMigrationsHistory", schema)).Options;
        await using var db = (TContext)Activator.CreateInstance(typeof(TContext), dbOptions)!;
        var transport = new EfSeedTransport<TContext>(db);
        return await runner.SeedAsync(contributor, transport, options, ct).ConfigureAwait(false);
    }

    private static async Task<SeedResult> RunApiAsync(
        SeedContributor contributor,
        ISeedRunner runner,
        SeedOptions options,
        string? apiBaseUrl,
        CancellationToken ct)
    {
        var baseUrl = string.IsNullOrWhiteSpace(apiBaseUrl) ? DefaultApiBaseUrl : apiBaseUrl.Trim();
        using var http = new HttpClient { BaseAddress = new(baseUrl.TrimEnd('/') + "/") };
        var client = new ApiClient(httpClient: http);
        var catalog = new SeedApiCatalog();
        catalog.Map<PersonSeedRequest>("Person");
        var transport = new ApiSeedTransport(client, catalog);
        return await runner.SeedAsync(contributor, transport, options, ct).ConfigureAwait(false);
    }
}
