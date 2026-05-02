using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Enums;
using Lyo.Api.Services.Crud.Read.Query;
using Lyo.Api.Services.Crud.Update;
using Lyo.Api.Tests.Fixtures;
using Lyo.Common.Enums;
using Lyo.Job.Models.Response;
using Lyo.Job.Postgres.Database;
using Lyo.Query.Models.Builders;
using Lyo.Query.Models.Common.Request;
using Lyo.Query.Models.Enums;

namespace Lyo.Api.Tests;

[Collection(ApiPostgresCollection.Name)]
public sealed class QueryCacheTagInvalidationPostgresTests
{
    private readonly ApiPostgresFixture _fixture;

    public QueryCacheTagInvalidationPostgresTests(ApiPostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Query_WithNestedInclude_InvalidatesWhenRootEntityPatched()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var defId = await _fixture.SeedJobDefinitionAsync($"QTagInv_{suffix}");
        await _fixture.SeedJobRunAsync(defId, $"before-{suffix}");
        using var scope = _fixture.ServiceProvider.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IQueryService<JobContext>>();
        var patchService = scope.ServiceProvider.GetRequiredService<IPatchService<JobContext>>();
        var request = new QueryReq {
            Start = 0,
            Amount = 10,
            Keys = [[defId]],
            Include = ["JobRuns"]
        };

        async Task<string?> NameFromQuery()
        {
            var result = await queryService.Query<JobDefinition>(request, x => x.Name, SortDirection.Asc, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess);
            return result.Items![0].Name;
        }

        Assert.Contains($"QTagInv_{suffix}", await NameFromQuery(), StringComparison.Ordinal);
        var patchRequest = new PatchRequest { Keys = [[defId]], Properties = new() { ["Name"] = $"QTagInv_renamed_{suffix}" } };
        var patchResult = await patchService.PatchAsync<JobDefinition, JobDefinitionRes>(patchRequest, ct: TestContext.Current.CancellationToken);
        Assert.Equal(PatchResultEnum.Updated, patchResult.Result);
        Assert.Equal($"QTagInv_renamed_{suffix}", await NameFromQuery());
    }

    [Fact]
    public async Task Query_WithNestedInclude_InvalidatesWhenIncludedEntityPatched()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var defId = await _fixture.SeedJobDefinitionAsync($"QTagCascade_{suffix}");
        var runId = await _fixture.SeedJobRunAsync(defId, $"before-{suffix}");
        using var scope = _fixture.ServiceProvider.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IQueryService<JobContext>>();
        var patchService = scope.ServiceProvider.GetRequiredService<IPatchService<JobContext>>();
        var request = new QueryReq {
            Start = 0,
            Amount = 10,
            Keys = [[defId]],
            Include = ["JobRuns"]
        };

        async Task<string?> CreatedByFromQuery()
        {
            var result = await queryService.Query<JobDefinition>(request, x => x.Name, SortDirection.Asc, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess);
            var def = result.Items![0];
            return def.JobRuns!.Single(r => r.Id == runId).CreatedBy;
        }

        Assert.Equal($"before-{suffix}", await CreatedByFromQuery());
        var patchRequest = new PatchRequest { Keys = [[runId]], Properties = new() { ["CreatedBy"] = $"after-{suffix}" } };
        var patchResult = await patchService.PatchAsync<JobRun, JobRunRes>(patchRequest, ct: TestContext.Current.CancellationToken);
        Assert.Equal(PatchResultEnum.Updated, patchResult.Result);
        Assert.Equal($"after-{suffix}", await CreatedByFromQuery());
    }

    [Fact]
    public async Task QueryProject_WithSelectOnNestedPath_InvalidatesWhenRelatedEntityPatched()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var defId = await _fixture.SeedJobDefinitionAsync($"QPTagInv_{suffix}");
        var runId = await _fixture.SeedJobRunAsync(defId, $"before-{suffix}");
        using var scope = _fixture.ServiceProvider.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IQueryService<JobContext>>();
        var patchService = scope.ServiceProvider.GetRequiredService<IPatchService<JobContext>>();
        var request = new ProjectionQueryReq {
            Start = 0,
            Amount = 10,
            Select = ["jobruns.createdby"],
            WhereClause = WhereClauseBuilder.Condition("Id", ComparisonOperatorEnum.Equals, defId)
        };

        async Task<string?> CreatedByFromProjection()
        {
            var result = await queryService.QueryProjected<JobDefinition>(request, x => x.Name, SortDirection.Asc, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess);
            var projected = Assert.IsType<List<object?>>(result.Items![0]);
            Assert.Single(projected);
            return projected[0]?.ToString();
        }

        Assert.Equal($"before-{suffix}", await CreatedByFromProjection());
        var patchRequest = new PatchRequest { Keys = [[runId]], Properties = new() { ["CreatedBy"] = $"after-{suffix}" } };
        var patchResult = await patchService.PatchAsync<JobRun, JobRunRes>(patchRequest, ct: TestContext.Current.CancellationToken);
        Assert.Equal(PatchResultEnum.Updated, patchResult.Result);
        Assert.Equal($"after-{suffix}", await CreatedByFromProjection());
    }

    [Fact]
    public async Task Get_WithNestedInclude_InvalidatesWhenRootEntityPatched()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var defId = await _fixture.SeedJobDefinitionAsync($"GTagInv_{suffix}");
        await _fixture.SeedJobRunAsync(defId, $"before-{suffix}");
        using var scope = _fixture.ServiceProvider.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IQueryService<JobContext>>();
        var patchService = scope.ServiceProvider.GetRequiredService<IPatchService<JobContext>>();

        async Task<string?> NameFromGet()
        {
            var entity = await queryService.Get<JobDefinition>([defId], ["JobRuns"], TestContext.Current.CancellationToken);
            Assert.NotNull(entity);
            return entity!.Name;
        }

        Assert.Contains($"GTagInv_{suffix}", await NameFromGet(), StringComparison.Ordinal);
        var patchRequest = new PatchRequest { Keys = [[defId]], Properties = new() { ["Name"] = $"GTagInv_renamed_{suffix}" } };
        var patchResult = await patchService.PatchAsync<JobDefinition, JobDefinitionRes>(patchRequest, ct: TestContext.Current.CancellationToken);
        Assert.Equal(PatchResultEnum.Updated, patchResult.Result);
        Assert.Equal($"GTagInv_renamed_{suffix}", await NameFromGet());
    }

    [Fact]
    public async Task Get_WithNestedInclude_InvalidatesWhenIncludedEntityPatched()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var defId = await _fixture.SeedJobDefinitionAsync($"GTagCascade_{suffix}");
        var runId = await _fixture.SeedJobRunAsync(defId, $"before-{suffix}");
        using var scope = _fixture.ServiceProvider.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IQueryService<JobContext>>();
        var patchService = scope.ServiceProvider.GetRequiredService<IPatchService<JobContext>>();

        async Task<string?> CreatedByFromGet()
        {
            var entity = await queryService.Get<JobDefinition>([defId], ["JobRuns"], TestContext.Current.CancellationToken);
            Assert.NotNull(entity);
            return entity!.JobRuns!.Single(r => r.Id == runId).CreatedBy;
        }

        Assert.Equal($"before-{suffix}", await CreatedByFromGet());
        var patchRequest = new PatchRequest { Keys = [[runId]], Properties = new() { ["CreatedBy"] = $"after-{suffix}" } };
        var patchResult = await patchService.PatchAsync<JobRun, JobRunRes>(patchRequest, ct: TestContext.Current.CancellationToken);
        Assert.Equal(PatchResultEnum.Updated, patchResult.Result);
        Assert.Equal($"after-{suffix}", await CreatedByFromGet());
    }
}