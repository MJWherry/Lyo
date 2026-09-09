using System.Net.Http.Json;
using System.Text.Json;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Common.Response;
using Lyo.Api.Models.Enums;
using Lyo.Api.Services.Crud.Read.Query;
using Lyo.Api.Services.Crud.Update;
using Lyo.Api.Tests.Fixtures;
using Lyo.Common.Core.Enums;
using Lyo.Cache;
using Lyo.Common.Json;
using Lyo.Job.Models.Response;
using Lyo.Job.Postgres.Database;
using Lyo.Query.Models.Builders;
using Lyo.Query.Models.Common.Request;
using Lyo.Query.Models.Enums;

namespace Lyo.Api.Tests;

/// <summary>
/// Exercises the three invalidation gaps where a cached read kept serving stale data after a write it should have noticed: a filter that only mentions a related type, a root
/// <c>/Query</c> page against a per-row patch, and a cached parent graph against a newly inserted child.
/// </summary>
[Collection(ApiPostgresCollection.Name)]
public sealed class QueryCacheInvalidationGapPostgresTests(ApiPostgresFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = LyoJsonSerializerOptions.Create();

    /// <summary>
    /// A query whose only mention of <c>JobRun</c> is in the where clause tagged itself with <c>JobDefinition</c> alone, so patching the run it filtered on never busted it.
    /// </summary>
    [Fact]
    public async Task Query_FilteringOnRelatedType_InvalidatesWhenThatTypeIsPatched()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var defId = await fixture.SeedJobDefinitionAsync($"FilterOnly_{suffix}");
        var runId = await fixture.SeedJobRunAsync(defId, $"match-{suffix}");
        using var scope = fixture.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IQueryService<JobContext>>();
        var patchService = scope.ServiceProvider.GetRequiredService<IPatchService<JobContext>>();

        // No Include and no Select touching JobRun: the relationship exists only inside the filter.
        var request = new QueryConcreteReq {
            Start = 0,
            Amount = 10,
            WhereClause = WhereClauseBuilder.And(
                b => b.Equals("Id", defId).Equals("JobRuns.CreatedBy", $"match-{suffix}"))
        };

        async Task<int> MatchCount()
        {
            var result = await queryService.Query<JobDefinition>(request, x => x.Name, SortDirection.Asc, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess, result.Error?.Detail);
            return result.Items!.Count;
        }

        Assert.Equal(1, await MatchCount());
        var patch = new PatchRequest { Keys = [[runId]], Properties = new() { ["CreatedBy"] = $"moved-{suffix}" } };
        var patchResult = await patchService.PatchAsync<JobRun, JobRunRes>(patch, ct: TestContext.Current.CancellationToken);
        Assert.Equal(PatchResultEnum.Updated, patchResult.Result);
        Assert.Equal(0, await MatchCount());
    }

    /// <summary>
    /// Root <c>/Query</c> pages carried only type tags, so under granular tagging. where a patch fires the row's instance tag instead of the type tag. nothing busted them.
    /// </summary>
    [Fact]
    public async Task RootQuery_UnderGranularTagging_InvalidatesWhenARowIsPatched()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        await using var granularFactory = new ApiWebApplicationFactory(
            fixture.ConnectionString, new Dictionary<string, string?> { ["CacheOptions:QueryCacheTagGranularity"] = nameof(QueryCacheTagGranularity.Granular) });

        using var client = granularFactory.CreateClient();
        var defId = await fixture.SeedJobDefinitionAsync($"RootGran_{suffix}");
        var request = QueryReqBuilder.New()
            .From("d", "JobDefinition")
            .AddSelects("d.Name")
            .AddWhere(w => w.Equals("d.Id", defId))
            .SetPagination(0, 5)
            .Build();

        async Task<string?> NameFromRootQuery()
        {
            var response = await client.PostAsJsonAsync("/api/Job/Query", request, JsonOptions, TestContext.Current.CancellationToken);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<ProjectedQueryRes<JsonElement>>(JsonOptions, TestContext.Current.CancellationToken);
            Assert.NotNull(result);
            Assert.True(result.IsSuccess, result.Error?.Detail);
            var row = Assert.Single(result.Items!);

            // A single-field root select projects to the scalar itself instead of a one-property object.
            return row.ValueKind == JsonValueKind.String
                ? row.GetString()
                : row.TryGetProperty("Name", out var name) || row.TryGetProperty("name", out name)
                    ? name.GetString()
                    : null;
        }

        Assert.Equal($"RootGran_{suffix}", await NameFromRootQuery());
        using var scope = granularFactory.Services.CreateScope();
        var patchService = scope.ServiceProvider.GetRequiredService<IPatchService<JobContext>>();
        var patch = new PatchRequest { Keys = [[defId]], Properties = new() { ["Name"] = $"RootGran_renamed_{suffix}" } };
        var patchResult = await patchService.PatchAsync<JobDefinition, JobDefinitionRes>(patch, ct: TestContext.Current.CancellationToken);
        Assert.Equal(PatchResultEnum.Updated, patchResult.Result);
        Assert.Equal($"RootGran_renamed_{suffix}", await NameFromRootQuery());
    }

    /// <summary>
    /// Creating a child emits the child's own type tag, and nothing carried the child's identity into the parent's cached graph, so the parent kept serving an include collection
    /// that was missing the row just inserted.
    /// </summary>
    [Fact]
    public async Task Query_WithIncludedChildren_InvalidatesWhenANewChildIsCreated()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var defId = await fixture.SeedJobDefinitionAsync($"NewChild_{suffix}");
        await fixture.SeedJobRunAsync(defId, $"first-{suffix}");
        using var scope = fixture.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IQueryService<JobContext>>();
        var request = new QueryConcreteReq {
            Start = 0,
            Amount = 10,
            Keys = [[defId]],
            Include = ["JobRuns"]
        };

        async Task<int> RunCount()
        {
            var result = await queryService.Query<JobDefinition>(request, x => x.Name, SortDirection.Asc, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess, result.Error?.Detail);
            return result.Items![0].JobRuns.Count;
        }

        Assert.Equal(1, await RunCount());
        await fixture.SeedJobRunAsync(defId, $"second-{suffix}");
        Assert.Equal(2, await RunCount());
    }
}
