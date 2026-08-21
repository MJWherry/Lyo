using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lyo.Api.ApiEndpoint;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Common.Response;
using Lyo.Api.Services.Cache;
using Lyo.Cache;
using Lyo.Common;
using Lyo.Common.Enums;
using Lyo.Query.Models.Builders;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;

namespace Lyo.Api.Tests;

public sealed class CacheEndpointTests
{
    private static readonly JsonSerializerOptions SerializerOptions = LyoJsonSerializerOptions.Create();

    [Fact]
    public void QueryProjected_FiltersSortsAndPages_WithoutCaching()
    {
        var (cache, service) = CreateQueryService();
        cache.Set("alpha-key", "a", ["alpha-tag"]);
        cache.Set("beta-key", "b", ["beta-tag"]);
        cache.Set("gamma-key", "c", ["gamma-tag"]);
        var query = ProjectionQueryReqBuilder.New()
            .AddSelects("Name", "Type", "Created", "Expires", "Encrypted", "Compressed", "SizeBytes")
            .AddWhere(w => w.Equals("Type", CacheItemTypeEnum.Key))
            .AddSort("Name", SortDirection.Asc)
            .SetPagination(1, 1)
            .Build();

        var result = service.QueryProjected(query);
        Assert.True(result.IsSuccess, result.Error?.Detail);
        Assert.Equal(1, result.Items?.Count);
        Assert.Equal(3, result.Total);
        Assert.True(result.HasMore);
        var row = Assert.IsType<Dictionary<string, object?>>(result.Items![0]);
        Assert.Equal("beta-key", row["Name"]?.ToString(), ignoreCase: true);
        Assert.True(row.ContainsKey("Encrypted"));
        Assert.True(row.ContainsKey("Compressed"));
        Assert.True(row.ContainsKey("SizeBytes"));
        Assert.True(row.ContainsKey("Expires"));
        Assert.NotNull(row["Expires"]);
        // WhereClause compiles through ICacheService.GetOrSet; that is not the admin QueryProject payload.
        Assert.Contains(cache.Items, i => string.Equals(i.Name, "beta-key", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(cache.Items, i => i.Name.Contains("QueryProject", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void QueryProjected_ContainsOnName_FiltersKeys()
    {
        var (cache, service) = CreateQueryService();
        cache.Set("alpha-key", "a");
        cache.Set("beta-key", "b");
        var query = ProjectionQueryReqBuilder.New()
            .AddSelects("Name", "Type")
            .AddWhere(w => w.Contains("Name", "beta"))
            .AddSort("Name", SortDirection.Asc)
            .SetPagination(0, 25)
            .Build();

        var result = service.QueryProjected(query);
        Assert.True(result.IsSuccess, result.Error?.Detail);
        var names = result.Items!.Select(i => Assert.IsType<Dictionary<string, object?>>(i)["Name"]?.ToString()).ToList();
        Assert.Contains(names, n => string.Equals(n, "beta-key", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(names, n => string.Equals(n, "alpha-key", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task QueryProjected_DoesNotAddAdminQueryCacheEntry()
    {
        var (cache, service) = CreateQueryService();
        cache.Set("keep-me", "value", ["keep"]);
        var namesBefore = cache.Items.Select(i => i.Name).OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToArray();
        var query = ProjectionQueryReqBuilder.New().AddSelects("Name", "Type").SetPagination(0, 25).Build();
        var result = service.QueryProjected(query);
        Assert.True(result.IsSuccess, result.Error?.Detail);
        var namesAfter = cache.Items.Select(i => i.Name).OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToArray();
        Assert.Equal(namesBefore, namesAfter);
    }

    [Fact]
    public async Task Mutations_ClearAndDeleteKeysAndTags()
    {
        var (cache, service) = CreateQueryService();
        cache.Set("k1", "v1", ["t1"]);
        cache.Set("k2", "v2", ["t2"]);
        cache.Set("k3", "v3", ["t2"]);
        var keysRemoved = await service.DeleteAsync(new DeleteRequest { Keys = [["Key", "k1"]] });
        Assert.True(keysRemoved.IsSuccess, keysRemoved.Error?.Detail);
        Assert.False(cache.TryGetValue<string>("k1", out _));
        var tagsRemoved = await service.DeleteBulkAsync([new DeleteRequest { Keys = [["Tag", "t2"], ["Tag", "__tag:t2"]], AllowMultiple = true }]);
        Assert.True(tagsRemoved.DeletedCount >= 1);
        Assert.False(cache.TryGetValue<string>("k2", out _));
        cache.Set("k4", "v4");
        var cleared = await service.ClearAsync();
        Assert.True(cleared.RemovedCount >= 1);
        Assert.Empty(cache.Items);
    }

    [Fact]
    public void NormalizeTagName_StripsKnownPrefixes()
    {
        Assert.Equal("queries", CacheQueryService.NormalizeTagName("__fc:t:queries"));
        Assert.Equal("queries", CacheQueryService.NormalizeTagName("__tag:queries"));
        Assert.Equal("queries", CacheQueryService.NormalizeTagName("queries"));
    }

    [Fact]
    public async Task MapCacheEndpoints_QueryProject_SetsNoStoreAndDoesNotCache()
    {
        await using var app = await StartAppAsync();
        var cache = app.Services.GetRequiredService<ICacheService>();
        cache.Set("http-key", "value", ["http-tag"]);
        var before = cache.Items.Count;
        var client = app.GetTestClient();
        var query = ProjectionQueryReqBuilder.New().AddSelects("Name", "Type", "Created").SetPagination(0, 25).Build();
        var response = await client.PostAsJsonAsync("/Cache/QueryProject", query, SerializerOptions, TestContext.Current.CancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"{response.StatusCode}: {responseBody}");
        var cacheControl = response.Headers.CacheControl?.ToString()
            ?? (response.Headers.TryGetValues("Cache-Control", out var values) ? string.Join(",", values) : "");
        Assert.Contains("no-store", cacheControl, StringComparison.OrdinalIgnoreCase);
        var body = JsonSerializer.Deserialize<ProjectedQueryRes<object?>>(responseBody, SerializerOptions);
        Assert.NotNull(body);
        Assert.True(body!.IsSuccess, body.Error?.Detail);
        Assert.True(body.Items?.Count >= 1);
        Assert.Equal(before, cache.Items.Count);
    }

    [Fact]
    public async Task MapCacheEndpoints_Mutations_RoundTrip()
    {
        await using var app = await StartAppAsync();
        var cache = app.Services.GetRequiredService<ICacheService>();
        cache.Set("mk1", "v1", ["mt1"]);
        cache.Set("mk2", "v2", ["mt2"]);
        var client = app.GetTestClient();
        using (var deleteKey = new HttpRequestMessage(HttpMethod.Delete, "/Cache") { Content = JsonContent.Create(new DeleteRequest { Keys = [["Key", "mk1"]] }, options: SerializerOptions) }) {
            var removeKeys = await client.SendAsync(deleteKey, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, removeKeys.StatusCode);
        }

        Assert.False(cache.TryGetValue<string>("mk1", out _));
        using (var deleteTag = new HttpRequestMessage(HttpMethod.Delete, "/Cache/Bulk") {
                   Content = JsonContent.Create(new List<DeleteRequest> { new() { Keys = [["Tag", "mt2"]], AllowMultiple = true } }, options: SerializerOptions)
               }) {
            var removeTags = await client.SendAsync(deleteTag, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, removeTags.StatusCode);
        }

        Assert.False(cache.TryGetValue<string>("mk2", out _));
        cache.Set("mk3", "v3");
        var clear = await client.PostAsJsonAsync("/Cache/Clear", new { }, SerializerOptions, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, clear.StatusCode);
        Assert.Empty(cache.Items);
    }

    private static (ICacheService Cache, CacheQueryService Service) CreateQueryService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMemoryCache();
        services.AddLocalCache();
        services.AddLyoQueryServices();
        var provider = services.BuildServiceProvider();
        return (provider.GetRequiredService<ICacheService>(), provider.GetRequiredService<CacheQueryService>());
    }

    private static async Task<WebApplication> StartAppAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.ConfigureHttpJsonOptions(o => LyoJsonSerializerOptions.ApplyTo(o.SerializerOptions));
        builder.Services.AddLocalCache();
        builder.Services.AddLyoQueryServices();
        var app = builder.Build();
        app.MapCacheEndpoints("Cache");
        await app.StartAsync();
        return app;
    }
}
