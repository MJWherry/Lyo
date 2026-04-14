using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Enums;
using Lyo.Api.Services.Crud.Read.Query;
using Lyo.Api.Services.Crud.Update;
using Lyo.Common.Enums;
using Lyo.Api.Tests.Fixtures;
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
    public async Task Query_WithNestedInclude_InvalidatesWhenRelatedEntityPatched()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var defId = await _fixture.SeedJobDefinitionAsync($"QTagInv_{suffix}").ConfigureAwait(false);
        var runId = await _fixture.SeedJobRunAsync(defId, $"before-{suffix}").ConfigureAwait(false);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IQueryService<JobContext>>();
        var patchService = scope.ServiceProvider.GetRequiredService<IPatchService<JobContext>>();

        var request = new QueryReq {
            Start = 0,
            Amount = 10,
            Keys = [[defId]],
            Include = ["JobRuns"],
        };

        async Task<string?> CreatedByFromQuery()
        {
            var result = await queryService.Query<JobDefinition>(request, x => x.Name, SortDirection.Asc, TestContext.Current.CancellationToken).ConfigureAwait(false);
            Assert.True(result.IsSuccess);
            var def = result.Items![0];
            return def.JobRuns!.Single(r => r.Id == runId).CreatedBy;
        }

        Assert.Equal($"before-{suffix}", await CreatedByFromQuery().ConfigureAwait(false));

        var patchRequest = new PatchRequest { Keys = [[runId]], Properties = new Dictionary<string, object?> { ["CreatedBy"] = $"after-{suffix}" } };
        var patchResult = await patchService.PatchAsync<JobRun, JobRunRes>(patchRequest, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(PatchResultEnum.Updated, patchResult.Result);

        Assert.Equal($"after-{suffix}", await CreatedByFromQuery().ConfigureAwait(false));
    }

    [Fact]
    public async Task QueryProject_WithSelectOnNestedPath_InvalidatesWhenRelatedEntityPatched()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var defId = await _fixture.SeedJobDefinitionAsync($"QPTagInv_{suffix}").ConfigureAwait(false);
        var runId = await _fixture.SeedJobRunAsync(defId, $"before-{suffix}").ConfigureAwait(false);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IQueryService<JobContext>>();
        var patchService = scope.ServiceProvider.GetRequiredService<IPatchService<JobContext>>();

        var request = new ProjectionQueryReq {
            Start = 0,
            Amount = 10,
            Select = ["jobruns.createdby"],
            WhereClause = WhereClauseBuilder.Condition("Id", ComparisonOperatorEnum.Equals, defId),
        };

        async Task<string?> CreatedByFromProjection()
        {
            var result = await queryService.QueryProjected<JobDefinition>(request, x => x.Name, SortDirection.Asc, TestContext.Current.CancellationToken).ConfigureAwait(false);
            Assert.True(result.IsSuccess);
            var projected = Assert.IsType<List<object?>>(result.Items![0]);
            Assert.Single(projected);
            return projected[0]?.ToString();
        }

        Assert.Equal($"before-{suffix}", await CreatedByFromProjection().ConfigureAwait(false));

        var patchRequest = new PatchRequest { Keys = [[runId]], Properties = new Dictionary<string, object?> { ["CreatedBy"] = $"after-{suffix}" } };
        var patchResult = await patchService.PatchAsync<JobRun, JobRunRes>(patchRequest, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(PatchResultEnum.Updated, patchResult.Result);

        Assert.Equal($"after-{suffix}", await CreatedByFromProjection().ConfigureAwait(false));
    }

    [Fact]
    public async Task Get_WithNestedInclude_InvalidatesWhenRelatedEntityPatched()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var defId = await _fixture.SeedJobDefinitionAsync($"GTagInv_{suffix}").ConfigureAwait(false);
        var runId = await _fixture.SeedJobRunAsync(defId, $"before-{suffix}").ConfigureAwait(false);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IQueryService<JobContext>>();
        var patchService = scope.ServiceProvider.GetRequiredService<IPatchService<JobContext>>();

        async Task<string?> CreatedByFromGet()
        {
            var entity = await queryService.Get<JobDefinition>([defId], ["JobRuns"], TestContext.Current.CancellationToken).ConfigureAwait(false);
            Assert.NotNull(entity);
            return entity!.JobRuns!.Single(r => r.Id == runId).CreatedBy;
        }

        Assert.Equal($"before-{suffix}", await CreatedByFromGet().ConfigureAwait(false));

        var patchRequest = new PatchRequest { Keys = [[runId]], Properties = new Dictionary<string, object?> { ["CreatedBy"] = $"after-{suffix}" } };
        var patchResult = await patchService.PatchAsync<JobRun, JobRunRes>(patchRequest, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(PatchResultEnum.Updated, patchResult.Result);

        Assert.Equal($"after-{suffix}", await CreatedByFromGet().ConfigureAwait(false));
    }
}
