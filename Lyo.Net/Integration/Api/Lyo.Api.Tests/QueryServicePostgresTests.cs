using Lyo.Api.Services.Crud.Read;
using Lyo.Api.Services.Crud.Read.Query;
using Lyo.Api.Tests.Fixtures;
using Lyo.Common.Enums;
using Lyo.Job.Models.Response;
using Lyo.Job.Postgres.Database;
using Lyo.Query.Models.Builders;
using Lyo.Query.Models.Common.Request;
using Lyo.Query.Models.Enums;

namespace Lyo.Api.Tests;

[Collection(ApiPostgresCollection.Name)]
public class QueryServicePostgresTests
{
    private readonly ApiPostgresFixture _fixture;

    public QueryServicePostgresTests(ApiPostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Query_WithNoFilters_ReturnsAllJobDefinitions()
    {
        var defId = await _fixture.SeedJobDefinitionAsync("QueryTest1").ConfigureAwait(false);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IQueryService<JobContext>>();
        var request = new QueryReq { Start = 0, Amount = 500 };
        var result = await queryService.Query<JobDefinition, JobDefinitionRes>(request, x => x.Name, SortDirection.Asc, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Items);
        Assert.True(result.Items!.Count >= 1);
        Assert.True(result.Total >= 1);
        var match = result.Items!.FirstOrDefault(i => i.Id == defId);
        Assert.NotNull(match);
        Assert.Equal("QueryTest1", match!.Name);
    }

    [Fact]
    public async Task Query_WithFilter_ReturnsMatchingItems()
    {
        await _fixture.SeedJobDefinitionAsync("FilterTestA").ConfigureAwait(false);
        await _fixture.SeedJobDefinitionAsync("FilterTestB").ConfigureAwait(false);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IQueryService<JobContext>>();
        var request = new QueryReq { Start = 0, Amount = 10, WhereClause = WhereClauseBuilder.Condition("Name", ComparisonOperatorEnum.Equals, "FilterTestA") };
        var result = await queryService.Query<JobDefinition, JobDefinitionRes>(request, x => x.Name, SortDirection.Asc, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Items);
        Assert.All(result.Items!, i => Assert.Equal("FilterTestA", i.Name));
    }

    [Fact]
    public async Task QueryProjected_WithSelect_OnlyReturnsSelectedFields()
    {
        var name = $"Projected_{Guid.NewGuid():N}";
        var defId = await _fixture.SeedJobDefinitionAsync(name, "Projected description").ConfigureAwait(false);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IQueryService<JobContext>>();
        var request = new ProjectionQueryReq {
            Start = 0,
            Amount = 10,
            Select = ["Id", "Name"],
            WhereClause = WhereClauseBuilder.Condition("Id", ComparisonOperatorEnum.Equals, defId)
        };

        var result = await queryService.QueryProjected<JobDefinition>(request, x => x.Name, SortDirection.Asc, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Items);
        Assert.Single(result.Items!);
        var row = Assert.IsAssignableFrom<IReadOnlyDictionary<string, object?>>(result.Items![0]);
        Assert.True(row.ContainsKey("Id"));
        Assert.True(row.ContainsKey("Name"));
        Assert.False(row.ContainsKey("Description"));
        Assert.Equal(name, row["Name"]?.ToString());
    }

    [Fact]
    public async Task QueryProjected_WithoutSelect_ReturnsFailure()
    {
        using var scope = _fixture.ServiceProvider.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IQueryService<JobContext>>();
        var request = new ProjectionQueryReq { Start = 0, Amount = 10 };
        var result = await queryService.QueryProjected<JobDefinition>(request, x => x.Name, SortDirection.Asc, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task QueryProjected_WithCollectionNavigationPath_ReturnsProjectedNestedValues()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var defId = await _fixture.SeedJobDefinitionAsync($"ProjectedNested_{suffix}").ConfigureAwait(false);
        await _fixture.SeedJobRunAsync(defId, $"nested-user-{suffix}").ConfigureAwait(false);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IQueryService<JobContext>>();
        var request = new ProjectionQueryReq {
            Start = 0,
            Amount = 10,
            Select = ["jobruns.createdby"],
            WhereClause = WhereClauseBuilder.Condition("Id", ComparisonOperatorEnum.Equals, defId)
        };

        var result = await queryService.QueryProjected<JobDefinition>(request, x => x.Name, SortDirection.Asc, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Items);
        Assert.Single(result.Items!);
        var projected = Assert.IsType<List<object?>>(result.Items[0]);
        Assert.Contains(projected, v => string.Equals(v?.ToString(), $"nested-user-{suffix}", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task QueryProjected_WithWildcardPath_ReturnsProjectedObjectGraph()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var defId = await _fixture.SeedJobDefinitionAsync($"ProjectedWildcard_{suffix}").ConfigureAwait(false);
        await _fixture.SeedJobRunAsync(defId, $"wildcard-user-{suffix}").ConfigureAwait(false);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IQueryService<JobContext>>();
        var request = new ProjectionQueryReq {
            Start = 0,
            Amount = 10,
            Select = ["jobruns.*"],
            WhereClause = WhereClauseBuilder.Condition("Id", ComparisonOperatorEnum.Equals, defId)
        };

        var result = await queryService.QueryProjected<JobDefinition>(request, x => x.Name, SortDirection.Asc, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Items);
        Assert.Single(result.Items!);
        var projectedRuns = Assert.IsType<List<object?>>(result.Items[0]);
        Assert.NotEmpty(projectedRuns);
        var firstRun = projectedRuns.OfType<IReadOnlyDictionary<string, object?>>().FirstOrDefault();
        Assert.NotNull(firstRun);
        Assert.True(firstRun!.ContainsKey("CreatedBy"));
        Assert.False(firstRun.ContainsKey("JobDefinition"));
    }

    [Fact]
    public async Task QueryProjected_WithRootWildcard_FlattensToRootObjectWithoutWildcardKey()
    {
        var name = $"ProjectedRootWildcard_{Guid.NewGuid():N}";
        var defId = await _fixture.SeedJobDefinitionAsync(name, "Root wildcard description").ConfigureAwait(false);
        await _fixture.SeedJobRunAsync(defId, "root-wildcard-user").ConfigureAwait(false);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IQueryService<JobContext>>();
        var request = new ProjectionQueryReq {
            Start = 0,
            Amount = 10,
            Select = ["*"],
            WhereClause = WhereClauseBuilder.Condition("Id", ComparisonOperatorEnum.Equals, defId)
        };

        var result = await queryService.QueryProjected<JobDefinition>(request, x => x.Name, SortDirection.Asc, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Items);
        Assert.Single(result.Items!);
        var row = Assert.IsAssignableFrom<IReadOnlyDictionary<string, object?>>(result.Items[0]);
        Assert.False(row.ContainsKey("*"));
        Assert.True(row.ContainsKey("Id"));
        Assert.True(row.ContainsKey("Name"));
        Assert.False(row.ContainsKey("JobRuns"));
    }

    [Fact]
    public async Task QueryProjected_MatchedOnly_FiltersCollectionScalarProjectionByWhereClause()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var defId = await _fixture.SeedJobDefinitionAsync($"ProjectedMatchedOnlyScalar_{suffix}").ConfigureAwait(false);
        await _fixture.SeedJobRunAsync(defId, $"keep-{suffix}").ConfigureAwait(false);
        await _fixture.SeedJobRunAsync(defId, $"drop-{suffix}").ConfigureAwait(false);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IQueryService<JobContext>>();
        var request = new ProjectionQueryReq {
            Start = 0,
            Amount = 10,
            Options = new() { IncludeFilterMode = QueryIncludeFilterMode.MatchedOnly },
            Select = ["jobruns.createdby"],
            WhereClause = WhereClauseBuilder.And(and => and.AddCondition("jobruns.createdby", ComparisonOperatorEnum.Equals, $"keep-{suffix}").AddCondition("Id", ComparisonOperatorEnum.Equals, defId))
        };

        var result = await queryService.QueryProjected<JobDefinition>(request, x => x.Name, SortDirection.Asc, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Items);
        Assert.Single(result.Items!);
        var projected = Assert.IsType<List<object?>>(result.Items[0]);
        Assert.NotEmpty(projected);
        Assert.All(projected, v => Assert.Equal($"keep-{suffix}", v?.ToString()));
    }

    [Fact]
    public async Task QueryProjected_MatchedOnly_FiltersCollectionWildcardProjectionByWhereClause()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var defId = await _fixture.SeedJobDefinitionAsync($"ProjectedMatchedOnlyWildcard_{suffix}").ConfigureAwait(false);
        await _fixture.SeedJobRunAsync(defId, $"keep-{suffix}").ConfigureAwait(false);
        await _fixture.SeedJobRunAsync(defId, $"drop-{suffix}").ConfigureAwait(false);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IQueryService<JobContext>>();
        var request = new ProjectionQueryReq {
            Start = 0,
            Amount = 10,
            Options = new() { IncludeFilterMode = QueryIncludeFilterMode.MatchedOnly },
            Select = ["jobruns.*"],
            WhereClause = WhereClauseBuilder.And(and => and.AddCondition("jobruns.createdby", ComparisonOperatorEnum.Equals, $"keep-{suffix}").AddCondition("Id", ComparisonOperatorEnum.Equals, defId))
        };

        var result = await queryService.QueryProjected<JobDefinition>(request, x => x.Name, SortDirection.Asc, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Items);
        Assert.Single(result.Items!);
        var projected = Assert.IsType<List<object?>>(result.Items[0]);
        Assert.NotEmpty(projected);
        var runMaps = projected.OfType<IReadOnlyDictionary<string, object?>>().ToList();
        Assert.NotEmpty(runMaps);
        Assert.All(runMaps, run => Assert.Equal($"keep-{suffix}", run["CreatedBy"]?.ToString()));
    }

    [Fact]
    public async Task QueryProjected_WithReferenceNavigationToScalar_ReturnsProjectedValues()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var defName = $"RefNavDef_{suffix}";
        var defId = await _fixture.SeedJobDefinitionAsync(defName, "Ref nav description").ConfigureAwait(false);
        await _fixture.SeedJobRunAsync(defId, "ref-user").ConfigureAwait(false);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IQueryService<JobContext>>();
        var request = new ProjectionQueryReq {
            Start = 0,
            Amount = 10,
            Select = ["jobruns.jobdefinition.name"],
            WhereClause = WhereClauseBuilder.Condition("Id", ComparisonOperatorEnum.Equals, defId)
        };

        var result = await queryService.QueryProjected<JobDefinition>(request, x => x.Name, SortDirection.Asc, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Items);
        Assert.Single(result.Items!);
        var projected = Assert.IsType<List<object?>>(result.Items[0]);
        Assert.NotEmpty(projected);
        Assert.Contains(projected, v => string.Equals(v?.ToString(), defName, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task QueryProjected_WithDeepPath_ReturnsProjectedNestedValues()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var defId = await _fixture.SeedJobDefinitionAsync($"DeepPath_{suffix}").ConfigureAwait(false);
        var runId = await _fixture.SeedJobRunAsync(defId, "deep-user").ConfigureAwait(false);
        await _fixture.SeedJobRunLogAsync(runId, $"deep-log-msg-{suffix}").ConfigureAwait(false);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IQueryService<JobContext>>();
        var request = new ProjectionQueryReq {
            Start = 0,
            Amount = 10,
            Select = ["jobruns.jobrunlogs.message"],
            WhereClause = WhereClauseBuilder.Condition("Id", ComparisonOperatorEnum.Equals, defId)
        };

        var result = await queryService.QueryProjected<JobDefinition>(request, x => x.Name, SortDirection.Asc, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Items);
        Assert.Single(result.Items!);
        var projected = Assert.IsType<List<object?>>(result.Items[0]);
        Assert.NotEmpty(projected);
        var messages = FlattenStrings(projected);
        Assert.Contains(messages, m => m.Contains($"deep-log-msg-{suffix}"));
    }

    private static List<string> FlattenStrings(IEnumerable<object?> values)
    {
        var list = new List<string>();
        foreach (var v in values) {
            if (v is string s)
                list.Add(s);
            else if (v is IEnumerable<object?> en)
                list.AddRange(FlattenStrings(en));
        }

        return list;
    }

    [Fact]
    public async Task QueryProjected_WithEmptyCollection_ReturnsEmptyArray()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var defId = await _fixture.SeedJobDefinitionAsync($"EmptyCollection_{suffix}").ConfigureAwait(false);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IQueryService<JobContext>>();
        var request = new ProjectionQueryReq {
            Start = 0,
            Amount = 10,
            Select = ["jobruns.createdby"],
            WhereClause = WhereClauseBuilder.Condition("Id", ComparisonOperatorEnum.Equals, defId)
        };

        var result = await queryService.QueryProjected<JobDefinition>(request, x => x.Name, SortDirection.Asc, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Items);
        Assert.Single(result.Items!);
        var projected = Assert.IsType<List<object?>>(result.Items[0]);
        Assert.Empty(projected);
    }

    [Fact]
    public async Task QueryProjected_WithKeysAndSelect_ReturnsProjectedRows()
    {
        var name = $"KeysSelect_{Guid.NewGuid():N}";
        var defId = await _fixture.SeedJobDefinitionAsync(name, "Keys+Select test").ConfigureAwait(false);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IQueryService<JobContext>>();
        var request = new ProjectionQueryReq {
            Keys = [[defId]],
            Start = 0,
            Amount = 10,
            Select = ["Id", "Name"]
        };

        var result = await queryService.QueryProjected<JobDefinition>(request, x => x.Name, SortDirection.Asc, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Items);
        Assert.Single(result.Items!);
        var row = Assert.IsAssignableFrom<IReadOnlyDictionary<string, object?>>(result.Items[0]);
        Assert.Equal(defId, row["Id"]);
        Assert.Equal(name, row["Name"]?.ToString());
        Assert.False(row.ContainsKey("Description"));
    }

    [Fact]
    public async Task QueryProjected_WithSimpleRootFields_ReturnsProjectedRows()
    {
        var name = $"SimpleRoot_{Guid.NewGuid():N}";
        var defId = await _fixture.SeedJobDefinitionAsync(name).ConfigureAwait(false);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IQueryService<JobContext>>();
        var request = new ProjectionQueryReq {
            Start = 0,
            Amount = 10,
            Select = ["Id", "Name"],
            WhereClause = WhereClauseBuilder.Condition("Id", ComparisonOperatorEnum.Equals, defId)
        };

        var result = await queryService.QueryProjected<JobDefinition>(request, x => x.Name, SortDirection.Asc, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Items);
        Assert.Single(result.Items!);
        var row = Assert.IsAssignableFrom<IReadOnlyDictionary<string, object?>>(result.Items[0]);
        Assert.Equal(defId, row["Id"]);
        Assert.Equal(name, row["Name"]?.ToString());
    }

    [Fact]
    public async Task QueryProjected_WithSelectAndComputedField_FormatsOutputAndStripsDependencyColumns()
    {
        var name = $"CfProj_{Guid.NewGuid():N}";
        var defId = await _fixture.SeedJobDefinitionAsync(name).ConfigureAwait(false);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IQueryService<JobContext>>();
        var request = new ProjectionQueryReq {
            Start = 0,
            Amount = 10,
            Select = ["Name"],
            ComputedFields = [new("Label", "{type}, {name}")],
            WhereClause = WhereClauseBuilder.Condition("Id", ComparisonOperatorEnum.Equals, defId)
        };

        var result = await queryService.QueryProjected<JobDefinition>(request, x => x.Name, SortDirection.Asc, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Items);
        Assert.Single(result.Items!);
        Assert.Single(result.QueryRequest.Select);
        Assert.Equal("Name", result.QueryRequest.Select[0]);
        var row = Assert.IsAssignableFrom<IReadOnlyDictionary<string, object?>>(result.Items[0]);
        Assert.Equal(name, row["Name"]?.ToString());
        Assert.Equal($"Test, {name}", row["Label"]?.ToString());
        Assert.False(row.ContainsKey("Type"));
    }

    [Fact]
    public async Task QueryProjected_WithInvalidSelectPath_ReturnsFailure()
    {
        var defId = await _fixture.SeedJobDefinitionAsync("InvalidPathTest").ConfigureAwait(false);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IQueryService<JobContext>>();
        var request = new ProjectionQueryReq {
            Start = 0,
            Amount = 10,
            Select = ["nonexistent.field"],
            WhereClause = WhereClauseBuilder.Condition("Id", ComparisonOperatorEnum.Equals, defId)
        };

        var result = await queryService.QueryProjected<JobDefinition>(request, x => x.Name, SortDirection.Asc, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task QueryProjected_WithMultipleRows_ReturnsProjectedForEach()
    {
        var name1 = $"MultiRow1_{Guid.NewGuid():N}";
        var name2 = $"MultiRow2_{Guid.NewGuid():N}";
        await _fixture.SeedJobDefinitionAsync(name1).ConfigureAwait(false);
        await _fixture.SeedJobDefinitionAsync(name2).ConfigureAwait(false);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IQueryService<JobContext>>();
        var request = new ProjectionQueryReq {
            Start = 0,
            Amount = 10,
            Select = ["Name"],
            WhereClause = WhereClauseBuilder.Or(or => or.Equals("Name", name1).Equals("Name", name2))
        };

        var result = await queryService.QueryProjected<JobDefinition>(request, x => x.Name, SortDirection.Asc, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Items);
        Assert.Equal(2, result.Items!.Count);
        var names = result.Items!.Select(i => i?.ToString()).OrderBy(n => n).ToList();
        Assert.Contains(name1, names);
        Assert.Contains(name2, names);
    }

    [Fact]
    public async Task QueryProjected_ReferenceOnlyNavigation_ReturnsProjectedValue()
    {
        var defName = $"RefOnly_{Guid.NewGuid():N}";
        var defId = await _fixture.SeedJobDefinitionAsync(defName).ConfigureAwait(false);
        await _fixture.SeedJobRunAsync(defId, "ref-only-user").ConfigureAwait(false);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IQueryService<JobContext>>();
        var request = new ProjectionQueryReq {
            Start = 0,
            Amount = 10,
            Select = ["jobdefinition.name"],
            WhereClause = WhereClauseBuilder.Condition("JobDefinitionId", ComparisonOperatorEnum.Equals, defId)
        };

        var result = await queryService.QueryProjected<JobRun>(request, x => x.CreatedTimestamp, SortDirection.Desc, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Items);
        Assert.NotEmpty(result.Items!);
        var value = result.Items![0];
        Assert.Equal(defName, value?.ToString());
    }

    [Fact]
    public async Task QueryProjected_WithCountCollectionPath_ReturnsProjectedCount()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var defId = await _fixture.SeedJobDefinitionAsync($"CountProj_{suffix}").ConfigureAwait(false);
        await _fixture.SeedJobRunAsync(defId, "run-1").ConfigureAwait(false);
        await _fixture.SeedJobRunAsync(defId, "run-2").ConfigureAwait(false);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IQueryService<JobContext>>();
        var request = new ProjectionQueryReq {
            Start = 0,
            Amount = 10,
            Select = ["Id", "Name", "JobRuns.Count"],
            WhereClause = WhereClauseBuilder.Condition("Id", ComparisonOperatorEnum.Equals, defId)
        };

        var result = await queryService.QueryProjected<JobDefinition>(request, x => x.Name, SortDirection.Asc, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Items);
        Assert.Single(result.Items!);
        var row = Assert.IsAssignableFrom<IReadOnlyDictionary<string, object?>>(result.Items![0]);
        Assert.True(row.ContainsKey("JobRuns.Count"));
        Assert.Equal(2, Convert.ToInt32(row["JobRuns.Count"]));
    }

    [Fact]
    public async Task QueryProjected_WithCountAndOtherFields_ReturnsProjectedRows()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var defId = await _fixture.SeedJobDefinitionAsync($"CountFields_{suffix}").ConfigureAwait(false);
        await _fixture.SeedJobRunAsync(defId, "user-a").ConfigureAwait(false);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IQueryService<JobContext>>();
        var request = new ProjectionQueryReq {
            Start = 0,
            Amount = 10,
            Select = ["Id", "Name", "JobRuns.Count"],
            WhereClause = WhereClauseBuilder.Condition("Id", ComparisonOperatorEnum.Equals, defId)
        };

        var result = await queryService.QueryProjected<JobDefinition>(request, x => x.Name, SortDirection.Asc, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Items);
        Assert.Single(result.Items!);
        var row = Assert.IsAssignableFrom<IReadOnlyDictionary<string, object?>>(result.Items![0]);
        Assert.Equal(defId, row["Id"]);
        Assert.Equal($"CountFields_{suffix}", row["Name"]?.ToString());
        Assert.Equal(1, Convert.ToInt32(row["JobRuns.Count"]));
    }

    [Fact]
    public async Task QueryProjected_WithCountOnEmptyCollection_ReturnsZero()
    {
        var name = $"CountEmpty_{Guid.NewGuid():N}";
        var defId = await _fixture.SeedJobDefinitionAsync(name).ConfigureAwait(false);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IQueryService<JobContext>>();
        var request = new ProjectionQueryReq {
            Start = 0,
            Amount = 10,
            Select = ["Id", "JobRuns.Count"],
            WhereClause = WhereClauseBuilder.Condition("Id", ComparisonOperatorEnum.Equals, defId)
        };

        var result = await queryService.QueryProjected<JobDefinition>(request, x => x.Name, SortDirection.Asc, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Items);
        Assert.Single(result.Items!);
        var row = Assert.IsAssignableFrom<IReadOnlyDictionary<string, object?>>(result.Items![0]);
        Assert.Equal(0, Convert.ToInt32(row["JobRuns.Count"]));
    }

    [Fact]
    public async Task Query_MatchedOnly_FiltersIncludedCollectionByWhereClause()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var defId = await _fixture.SeedJobDefinitionAsync($"QueryMatchedOnlyInclude_{suffix}").ConfigureAwait(false);
        await _fixture.SeedJobRunAsync(defId, $"keep-{suffix}").ConfigureAwait(false);
        await _fixture.SeedJobRunAsync(defId, $"drop-{suffix}").ConfigureAwait(false);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IQueryService<JobContext>>();
        var request = new QueryReq {
            Start = 0,
            Amount = 10,
            Include = ["JobRuns"],
            Options = new() { IncludeFilterMode = QueryIncludeFilterMode.MatchedOnly },
            WhereClause = WhereClauseBuilder.And(b => b.Equals("Id", defId).NotEquals("JobRuns.CreatedBy", $"drop-{suffix}"))
        };

        var result = await queryService.Query<JobDefinition>(request, x => x.Name, SortDirection.Asc, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Items);
        Assert.Single(result.Items!);
        var runs = result.Items![0].JobRuns.ToList();
        Assert.NotEmpty(runs);
        Assert.All(runs, run => Assert.Equal($"keep-{suffix}", run.CreatedBy));
    }

    [Fact]
    public async Task Query_MatchedOnly_WithOrOperator_FiltersIncludedCollectionToMatchingItemsOnly()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var defId = await _fixture.SeedJobDefinitionAsync($"QueryMatchedOnlyOr_{suffix}").ConfigureAwait(false);
        await _fixture.SeedJobRunAsync(defId, $"user-{suffix}@gmail.com").ConfigureAwait(false);
        await _fixture.SeedJobRunAsync(defId, $"user-{suffix}@yahoo.com").ConfigureAwait(false);
        await _fixture.SeedJobRunAsync(defId, $"user-{suffix}@charter.net").ConfigureAwait(false);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IQueryService<JobContext>>();
        var request = new QueryReq {
            Start = 0,
            Amount = 10,
            Include = ["JobRuns"],
            Options = new() { IncludeFilterMode = QueryIncludeFilterMode.MatchedOnly },
            WhereClause = WhereClauseBuilder.Or(b => b.EndsWith("JobRuns.CreatedBy", "@gmail.com", "@yahoo.com"))
        };

        var result = await queryService.Query<JobDefinition>(request, x => x.Name, SortDirection.Asc, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Items);
        Assert.Single(result.Items!);
        var runs = result.Items![0].JobRuns.ToList();
        Assert.Equal(2, runs.Count);
        Assert.All(runs, run => Assert.True(run.CreatedBy.EndsWith("@gmail.com") || run.CreatedBy.EndsWith("@yahoo.com"), $"Expected gmail or yahoo, got {run.CreatedBy}"));
    }

    [Fact]
    public async Task Query_WithKeys_ReturnsMatchingEntities()
    {
        var defId = await _fixture.SeedJobDefinitionAsync("KeysTest").ConfigureAwait(false);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IQueryService<JobContext>>();
        var request = new QueryReq { Keys = [[defId]], Start = 0, Amount = 10 };
        var result = await queryService.Query<JobDefinition, JobDefinitionRes>(request, x => x.Name, SortDirection.Asc, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Items);
        Assert.Single(result.Items!);
        Assert.Equal(defId, result.Items![0].Id);
    }

    [Fact]
    public async Task Query_WithKeysAndWhereClause_ReturnsOnlyMatchingEntity()
    {
        var defId = await _fixture.SeedJobDefinitionAsync("KeysNodeMatch").ConfigureAwait(false);
        await _fixture.SeedJobDefinitionAsync("KeysNodeOther").ConfigureAwait(false);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IQueryService<JobContext>>();
        var queryNode = WhereClauseBuilder.And(b => b.Equals("Name", "KeysNodeMatch"));
        var request = new QueryReq {
            Keys = [[defId]],
            Start = 0,
            Amount = 10,
            WhereClause = queryNode
        };

        var result = await queryService.Query<JobDefinition, JobDefinitionRes>(request, x => x.Name, SortDirection.Asc, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Items);
        Assert.Single(result.Items!);
        Assert.Equal(defId, result.Items![0].Id);
        Assert.Equal("KeysNodeMatch", result.Items![0].Name);
    }

    [Fact]
    public async Task Query_WithKeysAndWhereClause_ExcludesEntityWhenWhereClauseDoesNotMatch()
    {
        var defId = await _fixture.SeedJobDefinitionAsync("KeysNodeExclude").ConfigureAwait(false);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IQueryService<JobContext>>();
        var queryNode = WhereClauseBuilder.And(b => b.Equals("Name", "DifferentName"));
        var request = new QueryReq {
            Keys = [[defId]],
            Start = 0,
            Amount = 10,
            WhereClause = queryNode
        };

        var result = await queryService.Query<JobDefinition, JobDefinitionRes>(request, x => x.Name, SortDirection.Asc, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Items);
        Assert.Empty(result.Items!);
        Assert.Equal(0, result.Total);
    }

    [Fact]
    public async Task Query_WithKeysAndWhereClause_DoesNotReturnCachedUnfilteredResults()
    {
        await _fixture.SeedJobDefinitionAsync("KeysCacheA").ConfigureAwait(false);
        var defId = await _fixture.SeedJobDefinitionAsync("KeysCacheB").ConfigureAwait(false);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IQueryService<JobContext>>();
        var queryNode = WhereClauseBuilder.And(b => b.NotEquals("Name", null));
        var broadRequest = new QueryReq { Start = 0, Amount = 500, WhereClause = queryNode };
        var broadResult = await queryService.Query<JobDefinition, JobDefinitionRes>(broadRequest, x => x.Name, SortDirection.Asc, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(broadResult.IsSuccess);
        Assert.True(broadResult.Items!.Count >= 2);
        var keyedRequest = new QueryReq {
            Keys = [[defId]],
            Start = 0,
            Amount = 10,
            WhereClause = queryNode
        };

        var keyedResult = await queryService.Query<JobDefinition, JobDefinitionRes>(keyedRequest, x => x.Name, SortDirection.Asc, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(keyedResult.IsSuccess);
        Assert.Single(keyedResult.Items!);
        Assert.Equal(defId, keyedResult.Items![0].Id);
    }

    [Fact]
    public async Task Query_WithKeysAndInclude_DoesNotReturnCachedResultsFromUnkeyedQuery()
    {
        var defId = await _fixture.SeedJobDefinitionAsync("KeysIncludeCacheDef").ConfigureAwait(false);
        var runId = await _fixture.SeedJobRunAsync(defId).ConfigureAwait(false);
        var otherDefId = await _fixture.SeedJobDefinitionAsync("KeysIncludeCacheOther").ConfigureAwait(false);
        await _fixture.SeedJobRunAsync(otherDefId).ConfigureAwait(false);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IQueryService<JobContext>>();
        var unkeyedRequest = new QueryReq { Start = 0, Amount = 500, Include = ["JobDefinition"] };
        var unkeyedResult = await queryService.Query<JobRun, JobRunRes>(unkeyedRequest, x => x.CreatedTimestamp, SortDirection.Desc, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(unkeyedResult.IsSuccess);
        Assert.True(unkeyedResult.Items!.Count >= 2);
        var keyedRequest = new QueryReq {
            Keys = [[runId]],
            Start = 0,
            Amount = 10,
            Include = ["JobDefinition"]
        };

        var keyedResult = await queryService.Query<JobRun, JobRunRes>(keyedRequest, x => x.CreatedTimestamp, SortDirection.Desc, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(keyedResult.IsSuccess);
        Assert.Single(keyedResult.Items!);
        Assert.Equal(runId, keyedResult.Items![0].Id);
    }

    [Fact]
    public async Task Query_WithPagination_RespectsStartAndAmount()
    {
        await _fixture.SeedJobDefinitionAsync("Page1").ConfigureAwait(false);
        await _fixture.SeedJobDefinitionAsync("Page2").ConfigureAwait(false);
        await _fixture.SeedJobDefinitionAsync("Page3").ConfigureAwait(false);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IQueryService<JobContext>>();
        var request = new QueryReq { Start = 1, Amount = 1, WhereClause = WhereClauseBuilder.Condition("Name", ComparisonOperatorEnum.StartsWith, "Page") };
        var result = await queryService.Query<JobDefinition, JobDefinitionRes>(request, x => x.Name, SortDirection.Asc, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Items);
        Assert.Single(result.Items!);
        Assert.True(result.Total >= 3);
    }

    [Fact]
    public async Task Query_WithTotalCountModeNone_ReturnsNullTotal()
    {
        var prefix = $"CountModeNone_{Guid.NewGuid():N}";
        await _fixture.SeedJobDefinitionAsync($"{prefix}_A").ConfigureAwait(false);
        await _fixture.SeedJobDefinitionAsync($"{prefix}_B").ConfigureAwait(false);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IQueryService<JobContext>>();
        var request = new QueryReq {
            Start = 0,
            Amount = 1,
            Options = new() { TotalCountMode = QueryTotalCountMode.None },
            WhereClause = WhereClauseBuilder.Condition("Name", ComparisonOperatorEnum.StartsWith, prefix)
        };

        var result = await queryService.Query<JobDefinition, JobDefinitionRes>(request, x => x.Name, SortDirection.Asc, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Items);
        Assert.Single(result.Items!);
        Assert.Null(result.Total);
        Assert.Null(result.HasMore);
    }

    [Fact]
    public async Task Query_WithTotalCountModeHasMore_ReturnsUnknownTotalUntilLastPage()
    {
        var prefix = $"CountModeMore_{Guid.NewGuid():N}";
        await _fixture.SeedJobDefinitionAsync($"{prefix}_A").ConfigureAwait(false);
        await _fixture.SeedJobDefinitionAsync($"{prefix}_B").ConfigureAwait(false);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IQueryService<JobContext>>();
        var firstPageRequest = new QueryReq {
            Start = 0,
            Amount = 1,
            Options = new() { TotalCountMode = QueryTotalCountMode.HasMore },
            WhereClause = WhereClauseBuilder.Condition("Name", ComparisonOperatorEnum.StartsWith, prefix),
            SortBy = [new("Name", SortDirection.Asc)]
        };

        var firstPage = await queryService.Query<JobDefinition, JobDefinitionRes>(firstPageRequest, x => x.Name, SortDirection.Asc, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(firstPage.IsSuccess);
        Assert.NotNull(firstPage.Items);
        Assert.Single(firstPage.Items!);
        Assert.Null(firstPage.Total);
        Assert.True(firstPage.HasMore);
        var secondPageRequest = new QueryReq {
            Start = 1,
            Amount = 1,
            Options = new() { TotalCountMode = QueryTotalCountMode.HasMore },
            WhereClause = WhereClauseBuilder.Condition("Name", ComparisonOperatorEnum.StartsWith, prefix),
            SortBy = [new("Name", SortDirection.Asc)]
        };

        var secondPage = await queryService.Query<JobDefinition, JobDefinitionRes>(secondPageRequest, x => x.Name, SortDirection.Asc, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(secondPage.IsSuccess);
        Assert.NotNull(secondPage.Items);
        Assert.Single(secondPage.Items!);
        Assert.Equal(2, secondPage.Total);
        Assert.False(secondPage.HasMore);
    }

    [Fact]
    public async Task Query_WithSort_ReturnsOrderedResults()
    {
        await _fixture.SeedJobDefinitionAsync("SortZ").ConfigureAwait(false);
        await _fixture.SeedJobDefinitionAsync("SortA").ConfigureAwait(false);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IQueryService<JobContext>>();
        var request = new QueryReq {
            Start = 0,
            Amount = 10,
            WhereClause = WhereClauseBuilder.Condition("Name", ComparisonOperatorEnum.StartsWith, "Sort"),
            SortBy = [new("Name", SortDirection.Asc)]
        };

        var result = await queryService.Query<JobDefinition, JobDefinitionRes>(request, x => x.Name, SortDirection.Desc, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Items);
        Assert.True(result.Items!.Count >= 2);
        var names = result.Items!.Select(i => i.Name).ToList();
        var sorted = names.OrderBy(n => n).ToList();
        Assert.Equal(sorted, names);
    }

    [Fact]
    public async Task Get_ByKey_ReturnsEntity()
    {
        var defId = await _fixture.SeedJobDefinitionAsync("GetTest").ConfigureAwait(false);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IQueryService<JobContext>>();
        var result = await queryService.Get<JobDefinition, JobDefinitionRes>([defId], null, null, null, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.NotNull(result);
        Assert.Equal(defId, result!.Id);
        Assert.Equal("GetTest", result.Name);
    }

    [Fact]
    public async Task Get_ByKey_WhenNotFound_ReturnsNull()
    {
        using var scope = _fixture.ServiceProvider.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IQueryService<JobContext>>();
        var result = await queryService.Get<JobDefinition, JobDefinitionRes>([Guid.NewGuid()], null, null, null, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Null(result);
    }

    [Fact]
    public async Task Get_WithInclude_LoadsNavigation()
    {
        var defId = await _fixture.SeedJobDefinitionAsync("IncludeTest").ConfigureAwait(false);
        var runId = await _fixture.SeedJobRunAsync(defId).ConfigureAwait(false);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IQueryService<JobContext>>();
        var result = await queryService.Get<JobRun, JobRunRes>([runId], ["JobDefinition"], null, null, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.NotNull(result);
        Assert.Equal(runId, result!.Id);
        Assert.NotNull(result.JobDefinition);
        Assert.Equal(defId, result.JobDefinition!.Id);
    }

    [Fact]
    public async Task Query_WithInclude_LoadsNavigationProperties()
    {
        var defId = await _fixture.SeedJobDefinitionAsync("QueryIncludeDef").ConfigureAwait(false);
        var runId = await _fixture.SeedJobRunAsync(defId).ConfigureAwait(false);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IQueryService<JobContext>>();
        var request = new QueryReq {
            Start = 0,
            Amount = 10,
            Keys = [[runId]],
            Include = ["JobDefinition"]
        };

        var result = await queryService.Query<JobRun, JobRunRes>(request, x => x.CreatedTimestamp, SortDirection.Desc, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Items);
        Assert.Single(result.Items!);
        Assert.NotNull(result.Items![0].JobDefinition);
        Assert.Equal(defId, result.Items![0].JobDefinition!.Id);
    }

    [Fact]
    public async Task Query_WithAndFilter_MatchesAllConditions()
    {
        await _fixture.SeedJobDefinitionAsync("MultiGroupA").ConfigureAwait(false);
        await _fixture.SeedJobDefinitionAsync("MultiGroupB").ConfigureAwait(false);
        await _fixture.SeedJobDefinitionAsync("MultiGroupC").ConfigureAwait(false);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IQueryService<JobContext>>();
        var request = new QueryReq {
            Start = 0,
            Amount = 10,
            WhereClause = WhereClauseBuilder.Or(b => {
                b.AddCondition("Name", ComparisonOperatorEnum.Equals, "MultiGroupA");
                b.AddCondition("Name", ComparisonOperatorEnum.Equals, "MultiGroupB");
            })
        };

        var result = await queryService.Query<JobDefinition, JobDefinitionRes>(request, x => x.Name, SortDirection.Asc, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Items);
        Assert.Equal(2, result.Items!.Count);
        var names = result.Items!.Select(i => i.Name).OrderBy(n => n).ToList();
        Assert.Equal(["MultiGroupA", "MultiGroupB"], names);
    }

    [Fact]
    public async Task Query_WithMultipleFiltersInGroup_AndRequiresAll()
    {
        await _fixture.SeedJobDefinitionAsync("MultiFilterMatch", "desc").ConfigureAwait(false);
        await _fixture.SeedJobDefinitionAsync("MultiFilterNoMatch", "other").ConfigureAwait(false);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IQueryService<JobContext>>();
        var request = new QueryReq {
            Start = 0,
            Amount = 10,
            WhereClause = WhereClauseBuilder.And(b => {
                b.Equals("Name", "MultiFilterMatch");
                b.Equals("Description", "desc");
            })
        };

        var result = await queryService.Query<JobDefinition, JobDefinitionRes>(request, x => x.Name, SortDirection.Asc, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Items);
        Assert.Single(result.Items!);
        Assert.Equal("MultiFilterMatch", result.Items![0].Name);
        Assert.Equal("desc", result.Items![0].Description);
    }

    [Fact]
    public async Task Query_WithWhereClause_AppliesComplexTree()
    {
        await _fixture.SeedJobDefinitionAsync("NodeMatch").ConfigureAwait(false);
        await _fixture.SeedJobDefinitionAsync("NodeOther").ConfigureAwait(false);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IQueryService<JobContext>>();
        var queryNode = WhereClauseBuilder.And(b => b.Equals("Name", "NodeMatch").AddAnd(c => c.Equals("Type", "Test")));
        var request = new QueryReq { Start = 0, Amount = 10, WhereClause = queryNode };
        var result = await queryService.Query<JobDefinition, JobDefinitionRes>(request, x => x.Name, SortDirection.Asc, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Items);
        Assert.Single(result.Items!);
        Assert.Equal("NodeMatch", result.Items![0].Name);
    }

    [Fact]
    public async Task Query_WithWhereClauseOr_MatchesEitherCondition()
    {
        await _fixture.SeedJobDefinitionAsync("OrFirst").ConfigureAwait(false);
        await _fixture.SeedJobDefinitionAsync("OrSecond").ConfigureAwait(false);
        await _fixture.SeedJobDefinitionAsync("OrExcluded").ConfigureAwait(false);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IQueryService<JobContext>>();
        var queryNode = WhereClauseBuilder.Or(b => b.Equals("Name", "OrFirst").AddOr(c => c.Equals("Name", "OrSecond")));
        var request = new QueryReq { Start = 0, Amount = 10, WhereClause = queryNode };
        var result = await queryService.Query<JobDefinition, JobDefinitionRes>(request, x => x.Name, SortDirection.Asc, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Items);
        Assert.Equal(2, result.Items!.Count);
        var names = result.Items!.Select(i => i.Name).OrderBy(n => n).ToList();
        Assert.Equal(["OrFirst", "OrSecond"], names);
    }

    [Fact]
    public async Task Query_WithContains_ReturnsMatchingItems()
    {
        await _fixture.SeedJobDefinitionAsync("ContainsMiddle").ConfigureAwait(false);
        await _fixture.SeedJobDefinitionAsync("NoMatch").ConfigureAwait(false);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IQueryService<JobContext>>();
        var request = new QueryReq { Start = 0, Amount = 10, WhereClause = WhereClauseBuilder.Condition("Name", ComparisonOperatorEnum.Contains, "Middle") };
        var result = await queryService.Query<JobDefinition, JobDefinitionRes>(request, x => x.Name, SortDirection.Asc, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Items);
        Assert.Single(result.Items!);
        Assert.Equal("ContainsMiddle", result.Items![0].Name);
    }

    [Fact]
    public async Task Query_WithNotEquals_ExcludesMatching()
    {
        await _fixture.SeedJobDefinitionAsync("NotEqA").ConfigureAwait(false);
        await _fixture.SeedJobDefinitionAsync("NotEqB").ConfigureAwait(false);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IQueryService<JobContext>>();
        var request = new QueryReq {
            Start = 0,
            Amount = 10,
            WhereClause = WhereClauseBuilder.And(b => {
                b.AddCondition("Name", ComparisonOperatorEnum.StartsWith, "NotEq");
                b.AddCondition("Name", ComparisonOperatorEnum.NotEquals, "NotEqB");
            }),
            SortBy = [new("Name", SortDirection.Asc)]
        };

        var result = await queryService.Query<JobDefinition, JobDefinitionRes>(request, x => x.Name, SortDirection.Desc, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Items);
        Assert.Single(result.Items!);
        Assert.Equal("NotEqA", result.Items![0].Name);
    }

    [Fact]
    public async Task Query_WithIn_ReturnsMatchingItems()
    {
        await _fixture.SeedJobDefinitionAsync("InFirst").ConfigureAwait(false);
        await _fixture.SeedJobDefinitionAsync("InSecond").ConfigureAwait(false);
        await _fixture.SeedJobDefinitionAsync("InExcluded").ConfigureAwait(false);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IQueryService<JobContext>>();
        var request = new QueryReq {
            Start = 0,
            Amount = 10,
            WhereClause = WhereClauseBuilder.Condition("Name", ComparisonOperatorEnum.In, "InFirst,InSecond"),
            SortBy = [new("Name", SortDirection.Asc)]
        };

        var result = await queryService.Query<JobDefinition, JobDefinitionRes>(request, x => x.Name, SortDirection.Desc, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Items);
        Assert.Equal(2, result.Items!.Count);
        var names = result.Items!.Select(i => i.Name).OrderBy(n => n).ToList();
        Assert.Equal(["InFirst", "InSecond"], names);
    }

    [Fact]
    public async Task Query_WithNotContains_ExcludesMatching()
    {
        await _fixture.SeedJobDefinitionAsync("NC_Simple").ConfigureAwait(false);
        await _fixture.SeedJobDefinitionAsync("NC_HasXyzInName").ConfigureAwait(false);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IQueryService<JobContext>>();
        var request = new QueryReq {
            Start = 0,
            Amount = 10,
            WhereClause = WhereClauseBuilder.And(b => {
                b.AddCondition("Name", ComparisonOperatorEnum.StartsWith, "NC_");
                b.AddCondition("Name", ComparisonOperatorEnum.NotContains, "xyz");
            }),
            SortBy = [new("Name", SortDirection.Asc)]
        };

        var result = await queryService.Query<JobDefinition, JobDefinitionRes>(request, x => x.Name, SortDirection.Asc, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Items);
        Assert.Single(result.Items!);
        Assert.Equal("NC_Simple", result.Items![0].Name);
    }

    [Fact]
    public async Task Query_WithEndsWith_ReturnsMatchingItems()
    {
        await _fixture.SeedJobDefinitionAsync("EW_NameEndsWithSuffix").ConfigureAwait(false);
        await _fixture.SeedJobDefinitionAsync("EW_OtherName").ConfigureAwait(false);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IQueryService<JobContext>>();
        var request = new QueryReq {
            Start = 0,
            Amount = 10,
            WhereClause = WhereClauseBuilder.And(b => {
                b.AddCondition("Name", ComparisonOperatorEnum.StartsWith, "EW_");
                b.AddCondition("Name", ComparisonOperatorEnum.EndsWith, "Suffix");
            })
        };

        var result = await queryService.Query<JobDefinition, JobDefinitionRes>(request, x => x.Name, SortDirection.Asc, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Items);
        Assert.Single(result.Items!);
        Assert.Equal("EW_NameEndsWithSuffix", result.Items![0].Name);
    }

    [Fact]
    public async Task Query_WithNotStartsWith_ExcludesMatching()
    {
        await _fixture.SeedJobDefinitionAsync("Nsw_PrefixMatch").ConfigureAwait(false);
        await _fixture.SeedJobDefinitionAsync("Nsw_NotPrefixA").ConfigureAwait(false);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IQueryService<JobContext>>();
        var request = new QueryReq {
            Start = 0,
            Amount = 10,
            WhereClause = WhereClauseBuilder.And(b => {
                b.AddCondition("Name", ComparisonOperatorEnum.StartsWith, "Nsw_");
                b.AddCondition("Name", ComparisonOperatorEnum.NotStartsWith, "Nsw_Prefix");
            }),
            SortBy = [new("Name", SortDirection.Asc)]
        };

        var result = await queryService.Query<JobDefinition, JobDefinitionRes>(request, x => x.Name, SortDirection.Asc, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Items);
        Assert.Single(result.Items!);
        Assert.Equal("Nsw_NotPrefixA", result.Items![0].Name);
    }

    [Fact]
    public async Task Query_WithNotEndsWith_ExcludesMatching()
    {
        await _fixture.SeedJobDefinitionAsync("New_EndsWithA").ConfigureAwait(false);
        await _fixture.SeedJobDefinitionAsync("New_EndsWithB").ConfigureAwait(false);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IQueryService<JobContext>>();
        var request = new QueryReq {
            Start = 0,
            Amount = 10,
            WhereClause = WhereClauseBuilder.And(b => {
                b.AddCondition("Name", ComparisonOperatorEnum.StartsWith, "New_");
                b.AddCondition("Name", ComparisonOperatorEnum.NotEndsWith, "B");
            }),
            SortBy = [new("Name", SortDirection.Asc)]
        };

        var result = await queryService.Query<JobDefinition, JobDefinitionRes>(request, x => x.Name, SortDirection.Asc, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Items);
        Assert.Single(result.Items!);
        Assert.Equal("New_EndsWithA", result.Items![0].Name);
    }

    [Fact]
    public async Task Query_WithNotIn_ExcludesMatching()
    {
        await _fixture.SeedJobDefinitionAsync("Ni_First").ConfigureAwait(false);
        await _fixture.SeedJobDefinitionAsync("Ni_Second").ConfigureAwait(false);
        await _fixture.SeedJobDefinitionAsync("Ni_Excluded").ConfigureAwait(false);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IQueryService<JobContext>>();
        var request = new QueryReq {
            Start = 0,
            Amount = 10,
            WhereClause = WhereClauseBuilder.And(b => {
                b.AddCondition("Name", ComparisonOperatorEnum.StartsWith, "Ni_");
                b.AddCondition("Name", ComparisonOperatorEnum.NotIn, "Ni_First,Ni_Second");
            }),
            SortBy = [new("Name", SortDirection.Asc)]
        };

        var result = await queryService.Query<JobDefinition, JobDefinitionRes>(request, x => x.Name, SortDirection.Asc, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Items);
        Assert.Single(result.Items!);
        Assert.Equal("Ni_Excluded", result.Items![0].Name);
    }

    [Fact]
    public async Task Query_WithRegex_ReturnsMatchingItems()
    {
        await _fixture.SeedJobDefinitionAsync("Rx_RegexMatch123").ConfigureAwait(false);
        await _fixture.SeedJobDefinitionAsync("Rx_NoMatch").ConfigureAwait(false);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IQueryService<JobContext>>();
        var request = new QueryReq {
            Start = 0,
            Amount = 10,
            WhereClause = WhereClauseBuilder.And(b => {
                b.AddCondition("Name", ComparisonOperatorEnum.StartsWith, "Rx_");
                b.AddCondition("Name", ComparisonOperatorEnum.Regex, "^Rx_.*123$");
            })
        };

        var result = await queryService.Query<JobDefinition, JobDefinitionRes>(request, x => x.Name, SortDirection.Asc, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Items);
        Assert.Single(result.Items!);
        Assert.Equal("Rx_RegexMatch123", result.Items![0].Name);
    }

    [Fact]
    public async Task Query_WithNotRegex_ExcludesMatching()
    {
        await _fixture.SeedJobDefinitionAsync("Nrx_ExcludeMe").ConfigureAwait(false);
        await _fixture.SeedJobDefinitionAsync("Nrx_KeepMe").ConfigureAwait(false);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IQueryService<JobContext>>();
        var request = new QueryReq {
            Start = 0,
            Amount = 10,
            WhereClause = WhereClauseBuilder.And(b => {
                b.AddCondition("Name", ComparisonOperatorEnum.StartsWith, "Nrx_");
                b.AddCondition("Name", ComparisonOperatorEnum.NotRegex, "^Nrx_Exclude");
            }),
            SortBy = [new("Name", SortDirection.Asc)]
        };

        var result = await queryService.Query<JobDefinition, JobDefinitionRes>(request, x => x.Name, SortDirection.Asc, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Items);
        Assert.Single(result.Items!);
        Assert.Equal("Nrx_KeepMe", result.Items![0].Name);
    }

    [Fact]
    public async Task Query_WithGreaterThan_ReturnsMatchingItems()
    {
        var ts1 = new DateTime(2025, 2, 1, 10, 0, 0, DateTimeKind.Utc);
        var ts2 = new DateTime(2025, 2, 1, 12, 0, 0, DateTimeKind.Utc);
        await _fixture.SeedJobDefinitionAsync("GtTs_Early", createdTimestamp: ts1).ConfigureAwait(false);
        await _fixture.SeedJobDefinitionAsync("GtTs_Late", createdTimestamp: ts2).ConfigureAwait(false);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IQueryService<JobContext>>();
        var request = new QueryReq {
            Start = 0,
            Amount = 10,
            WhereClause = WhereClauseBuilder.And(b => {
                b.AddCondition("Name", ComparisonOperatorEnum.StartsWith, "GtTs_");
                b.AddCondition("CreatedTimestamp", ComparisonOperatorEnum.GreaterThan, new DateTime(2025, 2, 1, 11, 0, 0, DateTimeKind.Utc));
            }),
            SortBy = [new("Name", SortDirection.Asc)]
        };

        var result = await queryService.Query<JobDefinition, JobDefinitionRes>(request, x => x.Name, SortDirection.Asc, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Items);
        Assert.Single(result.Items!);
        Assert.Equal("GtTs_Late", result.Items![0].Name);
    }

    [Fact]
    public async Task Query_WithGreaterThanOrEqual_ReturnsMatchingItems()
    {
        var ts1 = new DateTime(2025, 2, 2, 10, 0, 0, DateTimeKind.Utc);
        var ts2 = new DateTime(2025, 2, 2, 12, 0, 0, DateTimeKind.Utc);
        await _fixture.SeedJobDefinitionAsync("GteTs_Early", createdTimestamp: ts1).ConfigureAwait(false);
        await _fixture.SeedJobDefinitionAsync("GteTs_Late", createdTimestamp: ts2).ConfigureAwait(false);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IQueryService<JobContext>>();
        var request = new QueryReq {
            Start = 0,
            Amount = 10,
            WhereClause = WhereClauseBuilder.And(b => {
                b.AddCondition("Name", ComparisonOperatorEnum.StartsWith, "GteTs_");
                b.AddCondition("CreatedTimestamp", ComparisonOperatorEnum.GreaterThanOrEqual, new DateTime(2025, 2, 2, 11, 0, 0, DateTimeKind.Utc));
            }),
            SortBy = [new("Name", SortDirection.Asc)]
        };

        var result = await queryService.Query<JobDefinition, JobDefinitionRes>(request, x => x.Name, SortDirection.Asc, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Items);
        Assert.Single(result.Items!);
        Assert.Equal("GteTs_Late", result.Items![0].Name);
    }

    [Fact]
    public async Task Query_WithLessThan_ReturnsMatchingItems()
    {
        var ts1 = new DateTime(2025, 2, 3, 10, 0, 0, DateTimeKind.Utc);
        var ts2 = new DateTime(2025, 2, 3, 12, 0, 0, DateTimeKind.Utc);
        await _fixture.SeedJobDefinitionAsync("LtTs_Early", createdTimestamp: ts1).ConfigureAwait(false);
        await _fixture.SeedJobDefinitionAsync("LtTs_Late", createdTimestamp: ts2).ConfigureAwait(false);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IQueryService<JobContext>>();
        var request = new QueryReq {
            Start = 0,
            Amount = 10,
            WhereClause = WhereClauseBuilder.And(b => {
                b.AddCondition("Name", ComparisonOperatorEnum.StartsWith, "LtTs_");
                b.AddCondition("CreatedTimestamp", ComparisonOperatorEnum.LessThan, new DateTime(2025, 2, 3, 11, 0, 0, DateTimeKind.Utc));
            }),
            SortBy = [new("Name", SortDirection.Asc)]
        };

        var result = await queryService.Query<JobDefinition, JobDefinitionRes>(request, x => x.Name, SortDirection.Asc, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Items);
        Assert.Single(result.Items!);
        Assert.Equal("LtTs_Early", result.Items![0].Name);
    }

    [Fact]
    public async Task Query_WithLessThanOrEqual_ReturnsMatchingItems()
    {
        var ts1 = new DateTime(2025, 2, 4, 10, 0, 0, DateTimeKind.Utc);
        var ts2 = new DateTime(2025, 2, 4, 12, 0, 0, DateTimeKind.Utc);
        await _fixture.SeedJobDefinitionAsync("LteTs_Early", createdTimestamp: ts1).ConfigureAwait(false);
        await _fixture.SeedJobDefinitionAsync("LteTs_Late", createdTimestamp: ts2).ConfigureAwait(false);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IQueryService<JobContext>>();
        var request = new QueryReq {
            Start = 0,
            Amount = 10,
            WhereClause = WhereClauseBuilder.And(b => {
                b.AddCondition("Name", ComparisonOperatorEnum.StartsWith, "LteTs_");
                b.AddCondition("CreatedTimestamp", ComparisonOperatorEnum.LessThanOrEqual, new DateTime(2025, 2, 4, 11, 0, 0, DateTimeKind.Utc));
            }),
            SortBy = [new("Name", SortDirection.Asc)]
        };

        var result = await queryService.Query<JobDefinition, JobDefinitionRes>(request, x => x.Name, SortDirection.Asc, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Items);
        Assert.Single(result.Items!);
        Assert.Equal("LteTs_Early", result.Items![0].Name);
    }

    [Fact]
    public async Task Query_TwoPhaseSubQuery_WithNestedChildren_ReturnsExpectedMatches()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var matchName = $"SubNested_Match_{suffix}";
        var nonMatchName = $"SubNested_NoMatch_{suffix}";
        var matchId = await _fixture.SeedJobDefinitionAsync(matchName).ConfigureAwait(false);
        var nonMatchId = await _fixture.SeedJobDefinitionAsync(nonMatchName).ConfigureAwait(false);
        await _fixture.SeedJobRunAsync(matchId, $"subquery-match-{suffix}").ConfigureAwait(false);
        await _fixture.SeedJobRunAsync(nonMatchId, $"subquery-other-{suffix}").ConfigureAwait(false);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IQueryService<JobContext>>();
        var nestedSubQuery = WhereClauseBuilder.And(b => {
            b.Add(
                WhereClauseBuilder.Or(or => {
                    or.Equals("Name", matchName);
                    or.AddAnd(and => and.Equals("JobRuns.CreatedBy", $"subquery-match-{suffix}"));
                }));
        });

        var withSubQuery = WhereClauseBuilder.ConditionWithSubClause("Enabled", ComparisonOperatorEnum.Equals, true, nestedSubQuery);
        var reqWithSub = new QueryReq {
            Start = 0,
            Amount = 100,
            WhereClause = withSubQuery,
            SortBy = [new("Name", SortDirection.Asc)]
        };

        var resWithSub = await queryService.Query<JobDefinition>(reqWithSub, x => x.Name, SortDirection.Asc, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(resWithSub.IsSuccess);
        var idsWithSub = resWithSub.Items!.Select(i => i.Id).OrderBy(i => i).ToList();
        Assert.Contains(matchId, idsWithSub);
        Assert.DoesNotContain(nonMatchId, idsWithSub);
    }

    [Fact]
    public async Task Query_TwoPhaseSubQuery_OnCollectionField_LoadsCollectionForInMemoryFilter()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var hitName = $"SubCollection_Hit_{suffix}";
        var missName = $"SubCollection_Miss_{suffix}";
        var hitId = await _fixture.SeedJobDefinitionAsync(hitName).ConfigureAwait(false);
        var missId = await _fixture.SeedJobDefinitionAsync(missName).ConfigureAwait(false);
        await _fixture.SeedJobRunAsync(hitId, $"created-by-{suffix}").ConfigureAwait(false);
        await _fixture.SeedJobRunAsync(missId, $"other-{suffix}").ConfigureAwait(false);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IQueryService<JobContext>>();
        var queryNode = WhereClauseBuilder.ConditionWithSubClause(
            "Enabled", ComparisonOperatorEnum.Equals, true, WhereClauseBuilder.And(b => b.Equals("JobRuns.CreatedBy", $"created-by-{suffix}")));

        var request = new QueryReq {
            Start = 0,
            Amount = 50,
            WhereClause = queryNode,
            SortBy = [new("Name", SortDirection.Asc)]
        };

        var result = await queryService.Query<JobDefinition>(request, x => x.Name, SortDirection.Asc, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Items);
        var ids = result.Items!.Select(i => i.Id).ToList();
        Assert.Contains(hitId, ids);
        Assert.DoesNotContain(missId, ids);
    }

    [Fact]
    public async Task Query_TwoPhaseSubQuery_WithSortAndPaging_ReturnsExpectedOrderedPage()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var names = new[] { $"SubOrder_C_{suffix}", $"SubOrder_A_{suffix}", $"SubOrder_B_{suffix}" };
        var ids = new List<Guid>();
        foreach (var name in names) {
            var id = await _fixture.SeedJobDefinitionAsync(name).ConfigureAwait(false);
            ids.Add(id);
            await _fixture.SeedJobRunAsync(id, $"order-{suffix}").ConfigureAwait(false);
        }

        using var scope = _fixture.ServiceProvider.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IQueryService<JobContext>>();
        var queryNode = WhereClauseBuilder.ConditionWithSubClause("Enabled", ComparisonOperatorEnum.Equals, true, WhereClauseBuilder.And(b => b.Equals("JobRuns.CreatedBy", $"order-{suffix}")));
        var request = new QueryReq {
            Start = 1,
            Amount = 1,
            WhereClause = queryNode,
            SortBy = [new("Name", SortDirection.Asc)],
            Options = new() { TotalCountMode = QueryTotalCountMode.Exact }
        };

        var result = await queryService.Query<JobDefinition>(request, x => x.Name, SortDirection.Desc, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Items);
        Assert.Single(result.Items!);
        var expectedSecond = names.OrderBy(x => x).Skip(1).First();
        Assert.Equal(expectedSecond, result.Items![0].Name);
        Assert.Equal(3, result.Total);
    }
}