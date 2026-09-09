using System.Net;
using System.Text;
using System.Text.Json;
using Lyo.Api.Client;
using Lyo.Api.Models.Common.Response;
using Lyo.Api.Models.Error;
using Lyo.Common.Json;
using Lyo.Query.Models.Common.Request;

namespace Lyo.Seed.Tests;

public sealed class ApiSeedTransportTests
{
    [Fact]
    public async Task PersistAsync_ChunksOverMaxAmount()
    {
        var handler = new StubHandler();
        handler.OnBulk = _ => OkBulk(created: 0, failed: 0, results: []);
        var transport = CreateTransport(handler, max: 2000);
        var items = Enumerable.Range(0, 2501).Select(i => new ItemReq { Name = $"n-{i}" }).ToArray();
        var contributor = new ItemContributor(items);
        var result = await new SeedRunner().SeedAsync(contributor, transport, new() { Conflict = SeedConflictMode.Append }, TestContext.Current.CancellationToken);
        Assert.True(result.Success);
        var bulks = handler.Requests.Where(r => r.Path.EndsWith("/Bulk", StringComparison.OrdinalIgnoreCase) && r.Method == "POST").ToList();
        Assert.Equal(2, bulks.Count);
        var first = JsonSerializer.Deserialize<List<ItemReq>>(bulks[0].Body, LyoJsonSerializerOptions.Create())!;
        var second = JsonSerializer.Deserialize<List<ItemReq>>(bulks[1].Body, LyoJsonSerializerOptions.Create())!;
        Assert.Equal(2000, first.Count);
        Assert.Equal(501, second.Count);
    }

    [Fact]
    public async Task SeedAsync_QueryConcreteOccupied_SkipsBulk()
    {
        var handler = new StubHandler();
        handler.OnQuery = () => OkQuery(total: 3);
        var transport = CreateTransport(handler);
        var result = await new SeedRunner().SeedAsync(
            new ItemContributor([new() { Name = "x" }]), transport, new() { Conflict = SeedConflictMode.SkipIfNotEmpty }, TestContext.Current.CancellationToken);
        Assert.True(result.Skipped);
        Assert.DoesNotContain(handler.Requests, r => r.Path.Contains("/Bulk", StringComparison.OrdinalIgnoreCase) && r.Method == "POST");
    }

    [Fact]
    public async Task PersistAsync_FailedCount_ReturnsSeedFailure()
    {
        var handler = new StubHandler();
        handler.OnQuery = () => OkQuery(total: 0);
        handler.OnBulk = _ => OkBulk(
            created: 0,
            failed: 1,
            results: [new CreateResult<ItemReq>(false, null, new LyoProblemDetails("row failed", 400, DateTime.UtcNow, []))]);
        var transport = CreateTransport(handler);
        var result = await new SeedRunner().SeedAsync(
            new ItemContributor([new() { Name = "x" }]), transport, new() { Conflict = SeedConflictMode.Append }, TestContext.Current.CancellationToken);
        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Contains("row failed", StringComparison.Ordinal));
    }

    private static ApiSeedTransport CreateTransport(StubHandler handler, int max = 2000)
    {
        var http = new HttpClient(handler) { BaseAddress = new("http://seed.test/") };
        var client = new ApiClient(httpClient: http, serializerOptions: LyoJsonSerializerOptions.Create());
        var catalog = new SeedApiCatalog { MaxBulkAmount = max };
        catalog.Map<ItemReq>("items");
        return new(client, catalog);
    }

    private static string OkQuery(int total)
    {
        var body = new QueryRes<ItemReq>(new QueryConcreteReq { Amount = 1 }, true, total == 0 ? [] : [new() { Name = "existing" }], 0, 1, total, false, 0, null);
        return JsonSerializer.Serialize(body, LyoJsonSerializerOptions.Create());
    }

    private static string OkBulk(int created, int failed, IReadOnlyList<CreateResult<ItemReq>> results)
    {
        var body = new CreateBulkResult<ItemReq>(results, created, failed);
        return JsonSerializer.Serialize(body, LyoJsonSerializerOptions.Create());
    }

    private sealed class ItemReq
    {
        public string Name { get; set; } = "";
    }

    private sealed class ItemContributor(IReadOnlyList<ItemReq> items) : SeedContributor
    {
        public override string Name => "items";

        public override SeedTransportKind SupportedTransports => SeedTransportKind.Api;

        protected override void Configure(SeedGraph graph, SeedOptions options) => graph.Entity(items);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        public List<(string Method, string Path, string Body)> Requests { get; } = [];

        public Func<string>? OnQuery { get; set; }

        public Func<string, string>? OnBulk { get; set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath.Trim('/') ?? "";
            var body = request.Content == null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add((request.Method.Method, path, body));
            string json;
            if (path.EndsWith("QueryConcrete", StringComparison.OrdinalIgnoreCase))
                json = OnQuery?.Invoke() ?? OkQuery(0);
            else if (path.EndsWith("Bulk", StringComparison.OrdinalIgnoreCase))
                json = OnBulk?.Invoke(body) ?? OkBulk(0, 0, []);
            else
                json = "{}";

            return new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        }
    }
}
