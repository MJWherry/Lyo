using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lyo.Api.Models.Common.Response;
using Lyo.Api.Tests.Fixtures;
using Lyo.Common;
using Lyo.Query.Models.Builders;
using Lyo.Query.Models.Common.Request;
using Lyo.Query.Models.Enums;

namespace Lyo.Api.Tests;

/// <summary>HTTP coverage for dynamic root <c>POST /api/Job/Query</c> (From/Joins).</summary>
[Collection(ApiPostgresCollection.Name)]
public sealed class RootQueryApiPostgresTests : IDisposable
{
    private const string RootQueryRoute = "/api/Job/Query";

    private static readonly JsonSerializerOptions JsonOptions = LyoJsonSerializerOptions.Create();

    private readonly HttpClient _client;
    private readonly ApiPostgresFixture _fixture;

    public RootQueryApiPostgresTests(ApiPostgresFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateClient();
    }

    public void Dispose() => _client.Dispose();

    [Fact]
    public async Task RootQuery_LeftJoin_ReturnsPeopleAndNestedRuns()
    {
        var name = $"RootJoin_{Guid.NewGuid():N}"[..20];
        var defId = await _fixture.SeedJobDefinitionAsync(name);
        await _fixture.SeedJobRunAsync(defId, "root-join-user");

        var request = QueryReqBuilder.New()
            .From("d", "JobDefinition")
            .Join(
                "r", "JobRun", JoinType.Left, on => {
                    on.Add(new JoinOn { From = "d.Id", To = "r.JobDefinitionId" });
                }, "r")
            .AddSelects("d.Name", "r.CreatedBy")
            .AddWhere(w => w.Equals("d.Id", defId))
            .SetPagination(0, 20)
            .SetTotalCountMode(QueryTotalCountMode.Exact)
            .Build();

        var response = await _client.PostAsJsonAsync(RootQueryRoute, request, JsonOptions, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ProjectedQueryRes<JsonElement>>(JsonOptions, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.True(result.IsSuccess, result.Error?.Detail);
        Assert.Equal(1, result.Total);
        var row = Assert.Single(result.Items!);
        Assert.Equal(JsonValueKind.Object, row.ValueKind);
        Assert.True(row.TryGetProperty("Name", out var nameProp) || row.TryGetProperty("name", out nameProp));
        Assert.Equal(name, nameProp.GetString());

        Assert.True(row.TryGetProperty("r", out var runs) || row.TryGetProperty("R", out runs));
        Assert.True(runs.ValueKind is JsonValueKind.Array or JsonValueKind.Object);
        if (runs.ValueKind == JsonValueKind.Array)
            Assert.NotEmpty(runs.EnumerateArray());
    }

    [Fact]
    public async Task RootQuery_NoJoin_ReturnsFlatProjection()
    {
        var name = $"RootFlat_{Guid.NewGuid():N}"[..20];
        var defId = await _fixture.SeedJobDefinitionAsync(name);

        var request = QueryReqBuilder.New()
            .From("d", "JobDefinition")
            .AddSelects("d.Name")
            .AddWhere(w => w.Equals("d.Id", defId))
            .SetPagination(0, 5)
            .Build();

        var response = await _client.PostAsJsonAsync(RootQueryRoute, request, JsonOptions, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ProjectedQueryRes<JsonElement>>(JsonOptions, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.Single(result.Items!);
    }

    [Fact]
    public async Task RootQuery_InvalidSelect_Returns400()
    {
        var request = QueryReqBuilder.New()
            .From("d", "JobDefinition")
            .AddSelects("d.NotARealColumn")
            .SetPagination(0, 5)
            .Build();

        var response = await _client.PostAsJsonAsync(RootQueryRoute, request, JsonOptions, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RootQuery_IncludeForbidden_Returns400()
    {
        var request = QueryReqBuilder.New().From("d", "JobDefinition").AddSelects("d.Name").Build();
        request.Include.Add("JobRuns");

        var response = await _client.PostAsJsonAsync(RootQueryRoute, request, JsonOptions, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RootQuery_UnknownEntity_Returns400()
    {
        var request = QueryReqBuilder.New().From("x", "MissingEntity").AddSelects("x.Id").Build();
        var response = await _client.PostAsJsonAsync(RootQueryRoute, request, JsonOptions, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RootQuery_Paging_IsFromSide()
    {
        var prefix = $"RootPage_{Guid.NewGuid():N}"[..12];
        await _fixture.SeedJobDefinitionAsync($"{prefix}_a");
        await _fixture.SeedJobDefinitionAsync($"{prefix}_b");
        await _fixture.SeedJobDefinitionAsync($"{prefix}_c");

        var request = QueryReqBuilder.New()
            .From("d", "JobDefinition")
            .Join(
                "r", "JobRun", JoinType.Left, on => {
                    on.Add(new JoinOn { From = "d.Id", To = "r.JobDefinitionId" });
                }, "r")
            .AddSelects("d.Name", "r.CreatedBy")
            .AddWhere(w => w.StartsWith("d.Name", prefix!))
            .AddSort("d.Name", Lyo.Common.Enums.SortDirection.Asc)
            .SetPagination(0, 2)
            .SetTotalCountMode(QueryTotalCountMode.Exact)
            .Build();

        var response = await _client.PostAsJsonAsync(RootQueryRoute, request, JsonOptions, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ProjectedQueryRes<JsonElement>>(JsonOptions, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Items!.Count);
        Assert.Equal(3, result.Total);
        Assert.True(result.HasMore);
    }
}
